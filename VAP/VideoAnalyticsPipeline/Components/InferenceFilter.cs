using KdTree.Math;
using KdTree;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Diagnostics.Metrics;

namespace VideoAnalyticsPipeline.Components;
internal class InferenceFilter(ModelConfig modelConfig, ILogger<InferenceFilter> logger)
{
    private const int kdTreeDimension = 2;
    private const float coordinateLowerBound = 0;
    private const float coordinateUpperBound = 1;

    // In this kdTree dictionary we store one kdtree for each class in a camera(key)
    private readonly ConcurrentDictionary<string, KdTree<float, Detection>> kdTree = [];
    private readonly ConcurrentDictionary<string, long> processedCoordinates = [];
    private readonly ConcurrentDictionary<string, long> counter = [];


    internal bool IfCoordinatesNotOutOfBounds(float[] coordinates)
    {
        var bounds = coordinates.All(c => c >= coordinateLowerBound && c <= coordinateUpperBound);

        if (!bounds)
            logger.LogError("Coordinate bounds {coordinates} are not within 0 and 1. This will not be processed further", coordinates);

        return bounds;
    }

    // To reduce load on pipeline we filter coordinates that are already processed within a time interval
    // The following rules are applied to check if the coordinates are already processed
    // A key is generated using the coordinates,camera serial, class id and confidence
    // Confidence is converted to a range - low if it is less than the model confidence, high otherwise
    // The key is then added to a dictionary with the timeStamp as the value
    // When a new set of coordinates is received, the key is checked in the dictionary
    // If the key is found and the timeStamp is within the interval, the coordinates are considered processed
    // If the key is found and the timeStamp is outside the interval, the coordinates are considered not processed and timestamp is updated
    // If deferred flag is set on any model, it means it should not be processed first time a violation is detected. It wil be processed on subsequent violations detected 

    internal bool IfCoordinatesNotProcessed(float[] coordinates, long timeStamp, string cameraSerial, int classId, float confidence)
    {
        var modelInference = modelConfig[classId];

        var key = GenerateKey(coordinates, cameraSerial, classId, confidence);
        if (processedCoordinates.TryGetValue(key, out var cachedTimestamp))
        {
            var timeout = modelConfig.Timeout(cameraSerial);

            if (timeStamp - cachedTimestamp > timeout)
            {
                processedCoordinates[key] = timeStamp;
                return true;
            }
            logger.LogWarning("Coordinates {coordinates} for camera {cameraSerial} for class {classId} with confidence {confidence} already processed at {cachedTimestamp}",
                coordinates, cameraSerial, classId, confidence, cachedTimestamp);
            return false;
        }
        else
        {
            if (processedCoordinates.TryAdd(key, timeStamp))
            {
                logger.LogInformation("Key {key} added to cache", key);
                if(modelInference.Deferred)
                {
                    logger.LogInformation("Coordinates {coordinates} for camera {cameraSerial} for class {classId} with confidence {confidence} has deferred flag set. Processing for violations will happen once it has crossed timeout threshold.", coordinates, cameraSerial, classId, confidence);
                    return false;
                }
                return true;
            }

            throw new InvalidOperationException("Failed to add coordinates to cache");
        }
    }
    internal bool IfCoordinatesNotNeighbours(Output output, string cameraSerial, long timestamp)
    {
        logger.LogInformation("Checking if coordinates {coordinates} are neighbours to already processed", output.Location);

        var modelInference = modelConfig[output.Class];

        var radiusLimit = modelConfig.RadiusLimit(cameraSerial);
        var timeout = modelConfig.Timeout(cameraSerial); ;

        var key = cameraSerial + output.Class;

        // Check if a Kd tree exists for the class in that camera; if not, create and add it to the dictionary
        if (!kdTree.TryGetValue(key, out var tree))
        {
            tree = new KdTree<float, Detection>(kdTreeDimension, new FloatMath());
            kdTree[key] = tree;
        }

        // Find the center of the bounding box
        var midPoint = MidPoint(output.Location!);

        // Perform radial search to find all the detections within the specified radius from the center
        var neighbors = tree.RadialSearch(midPoint, radiusLimit);

        // Check if any neighbor's timestamp is within the time constraint; if yes, ignore the current output
        if (neighbors.Length > 0 && neighbors.Any(n => timestamp - n.Value.Timestamp < timeout))
        {
            logger.LogWarning("Coordinates {coordinates} are neighbours to already processed", output.Location);
            return false;
        }

        // If no similar neighbors are found within the time constraint, add the current output to the tree
        var detection = new Detection
        {
            Output = output,
            Timestamp = timestamp,
        };

        tree.Add(midPoint, detection);

        logger.LogInformation("Coordinates {coordinates} are not neighbours to already processed and are added to KD Tree", output.Location);    

        return true;
    }

    internal bool IfCountIsNotCorrect(Output[] outputs, string cameraSerial, long timestamp)
    {
        // Check the count, if it is ok, we will return false
        // If it is not ok, we will try to see if there are any previous inference like this -
        // If the inference is not present in counter dict, we will add the current timestamp to the dict and we will not send the alert and return false (because we are having a cooling period)
        // If the inference is present in counter dict we will check the cachedtimestamp and curr. timestamp difference, if it  is greater than cooling period, we will generate an alert
        // Every time, if the output count is >= required count we will remove the camera in counter.

        if (outputs.Length >= modelConfig.Count(cameraSerial))
        {
            counter.TryRemove(cameraSerial, out var removedTimestamp);
            return false;
        }

        if (!counter.ContainsKey(cameraSerial))
        {
            counter[cameraSerial] = timestamp;
            return false;
        }

        if (timestamp - counter[cameraSerial] > modelConfig.CountTimeout(cameraSerial))
        {
            logger.LogInformation("Count in {Camera} is less than the required count {Count}", cameraSerial, modelConfig.Count(cameraSerial));
            return true;
        }

        return false;
    }
    internal string GenerateKey(float[] coordinates, string camSerial, int classId, float confidence)
    {
        var confidenceRange = confidence < modelConfig.ModelConfidence(classId) ? "low" : "high";
        var key = new StringBuilder();
        key.Append(string.Join(',', coordinates));
        key.Append(',');
        key.Append(camSerial);
        key.Append(',');
        key.Append(classId);
        key.Append(',');
        key.Append(confidenceRange);
        return key.ToString();
    }

    internal static float[] MidPoint(float[] coordinates)
    {
        //Format : [xmin, ymin, xmax, ymax]
        var x = (float)Math.Round((coordinates[0] + coordinates[2]) / 2, 3);
        var y = (float)Math.Round((coordinates[1] + coordinates[3]) / 2, 3);

        return [x, y];
    }
}
