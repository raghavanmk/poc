using KdTree.Math;
using KdTree;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline.Components;
internal class InferenceFilter(ModelConfig modelConfig, ILogger<InferenceFilter> logger)
{
    private const int kdTreeDimension = 2;
    private const float coordinateLowerBound = 0;
    private const float coordinateUpperBound = 1;

    // In this kdTree dictionary we store one kdtree for each class in a camera(key)
    private readonly Dictionary<string, KdTree<float, Detection>> kdTree = [];
    private readonly ConcurrentDictionary<string, long> processedCoordinates = [];

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

    internal bool IfCoordinatesNotProcessed(float[] coordinates, long timeStamp, string camSerial, int classId, float confidence)
    {
        var modelInference = modelConfig[camSerial!];

        var key = GenerateKey(coordinates, camSerial, classId, confidence);
        if (processedCoordinates.TryGetValue(key, out var cachedTimestamp))
        {
            if (timeStamp - cachedTimestamp > modelInference.Timeout)
            {
                processedCoordinates[key] = timeStamp;
                return true;
            }
            logger.LogWarning("Coordinates {coordinates} for camera {camSerial} for class {classId} with confidence {confidence} already processed at {cachedTimestamp}",
                coordinates, camSerial, classId, confidence, cachedTimestamp);
            return false;
        }
        else
        {
            if (processedCoordinates.TryAdd(key, timeStamp))
                if (modelInference.Deferred) return false;
                else return true;
            throw new InvalidOperationException("Failed to add coordinates to cache");
        }
    }
    internal bool IfCoordinatesNotNeighbours(Output output, string cameraSerial, long timestamp)
    {
        var modelInference = modelConfig[cameraSerial];
     
        var key = cameraSerial + output.Class;

        // Check if a Kd tree exists for the class in that camera; if not, create and add it to the dictionary
        if (!kdTree.TryGetValue(key, out var tree))
        {
            tree = new KdTree<float, Detection>(kdTreeDimension, new FloatMath());
            kdTree[cameraSerial + output.Class] = tree;
        }

        // Find the center of the bounding box
        var midPoint = MidPoint(output.Location!);

        // Perform radial search to find all the detections within the specified radius from the center
        var neighbors = tree.RadialSearch(midPoint, modelInference.RadiusLimit);

        // Check if any neighbor's timestamp is within the time constraint; if yes, ignore the current output
        if (neighbors.Length > 0 && neighbors.Any(n => timestamp - n.Value.Timestamp < modelInference.Timeout))
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
        return true;
    }

    internal string GenerateKey(float[] coordinates, string camSerial, int classId, float confidence)
    {
        var confidenceRange = confidence < modelConfig.ModelConfidence(camSerial) ? "low" : "high";
        var key = string.Join(',', coordinates) + "," + camSerial + "," + classId + "," + confidenceRange;
        return key;
    }

    internal static float[] MidPoint(float[] coordinates)
    {
        //Format : [ymin, xmin, ymax, xmax]
        var x = (float)Math.Round((coordinates[1] + coordinates[3]) / 2, 3);
        var y = (float)Math.Round((coordinates[0] + coordinates[2]) / 2, 3);

        return [x, y];
    }
}
