using Microsoft.Extensions.Logging;
using KdTree;
using KdTree.Math;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace VideoAnalyticsPipeline;

internal class InferenceRules(ModelConfig modelConfig, ILogger<InferenceRules> logger, IConfiguration configuration)
{
    private const int kdTreeDimension = 2;
    private const float coordinateLowerBound = 0;
    private const float coordinateUpperBound = 1;

    private readonly Dictionary<string, KdTree<float, Detection>> kdTree = [];
    private readonly ConcurrentDictionary<string, long> processedCoordinates = [];    
    private readonly float radiusLimit = configuration.GetValue<float>("FilteringRules:RadiusLimit");
    private readonly int timeout = configuration.GetValue<int>("FilteringRules:Timeout");

    internal bool TryDetectViolation(Data data, out Output[] violations)
    {
        violations = data.Inference?.Outputs?.Where(o => IsValidOutput(o, data)).ToArray() ?? [];
        return violations.Length > 0;
    }

    private bool IsValidOutput(Output output, Data data) =>
        
        IfCoordinatesNotOutOfBounds(output.Location!) &&
        IfCoordinatesNotProcessed(output.Location!, data.Inference!.Timestamp, data.CameraSerial!, output.Class, output.Score) &&
        IfCoordinatesNotNeighbours(output, data.CameraSerial!, data.Inference.Timestamp) &&
        IfInferenceOutsideThreshold(modelConfig[data.CameraSerial!], output);


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
        var key = GenerateKey(coordinates, camSerial, classId, confidence);
        if (processedCoordinates.TryGetValue(key, out var cachedTimestamp))
        {
            if (timeStamp - cachedTimestamp > timeout)
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
                return true;
            throw new InvalidOperationException("Failed to add coordinates to cache");
        }
    }
    internal bool IfCoordinatesNotNeighbours(Output output, string cameraSerial, long timestamp)
    {        
        // should class id be considered ?

        if (!kdTree.TryGetValue(cameraSerial, out var tree))
        {
            tree = new KdTree<float, Detection>(kdTreeDimension, new FloatMath());
            kdTree[cameraSerial] = tree;
        }
        var midPoint = MidPoint(output.Location!);
        var neighbors = tree.RadialSearch(midPoint, radiusLimit);

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
        return true;
    }

    internal static bool IfInferenceOutsideThreshold(ModelInference modelInference, Output output) =>

        modelInference!.Class!.Contains(output.Class) &&
               output.Score > modelInference.Confidence;


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
