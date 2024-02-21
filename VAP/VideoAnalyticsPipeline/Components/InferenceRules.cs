using Microsoft.Extensions.Logging;
using KdTree;
using KdTree.Math;
using Microsoft.Extensions.Configuration;

namespace VideoAnalyticsPipeline;

public class InferenceRules(ModelConfig modelConfig, InferenceCache cache, ILogger<InferenceRules> logger, IConfiguration configuration)
{
    // In kdTreeDict we store one kdtree for each camera(key)
    // KdTree<float, Detection> here float is the point, detection can be any value that can be stored for that point
    private readonly IDictionary<string, KdTree<float, Detection>> kdTreeDict = new Dictionary<string, KdTree<float, Detection>>();
    public bool TryDetectViolation(Data data, out Output[] violations)
    {
        var modelInference = modelConfig[data.CameraSerial!];

        violations = data.Inference?.Outputs?.Where(o =>
                        CheckCoordinateBounds(o.Location)
                        && !cache.TryCheckIfCoordinatesAreProcessed(o.Location, data.Inference.Timestamp, data.CameraSerial!,o.Class,o.Score)
                        && modelInference!.Class!.Contains(o.Class)
                        && o.Score > modelInference.Confidence
                        && !CheckIfNeighborsAreSimilar(o, data.CameraSerial!, data.Inference.Timestamp)).ToArray()
                        ?? [];

        return violations.Length > 0;
    }
    public bool CheckIfNeighborsAreSimilar(Output output, string cameraSerial, long timestamp)
    {
        // Check if a KD tree exists for the camera; if not, create and add it to the dictionary
        if (!kdTreeDict.TryGetValue(cameraSerial, out var tree))
        {
            tree = new KdTree<float, Detection>(2, new FloatMath());
            kdTreeDict[cameraSerial] = tree;
        }

        // Finding the centre of the bounding box
        var centre = CoordsCentre(output.Location!);

        // Radial search to find all detections within the specified radius from the center of the bounding box
        var neighbors = tree.RadialSearch(centre, configuration.GetValue<float>("InferenceCache:RadiusLimit")).ToArray();

        // Check if any neighbor's timestamp is within the time constraint; if yes, ignore the current output
        if (neighbors.Length > 0 && neighbors.Any(n => timestamp - n.Value.Timestamp < configuration.GetValue<float>("InferenceCache:Timeout"))) return true;

        // If no similar neighbors are found within the time constraint, add the current output to the tree
        var detection = new Detection
        {
            Output = output,
            Timestamp = timestamp,
        };

        tree.Add(centre, detection);
        return false;
    }

    bool CheckCoordinateBounds(float[] coordinates)
    {
        var bounds = coordinates.All(c => c >= 0 && c <= 1);

        if (!bounds)
            logger.LogError("Coordinate bounds {coordinates} are not within 0 and 1. This will not be processed further", coordinates);

        return bounds;
    }

    float[] CoordsCentre(float[] coordinates)
    {
        //Format : [ymin, xmin, ymax, xmax]
        var x = (float)Math.Round((coordinates[1] + coordinates[3]) / 2, 3);
        var y = (float)Math.Round((coordinates[0] + coordinates[2]) / 2, 3);

        return [x, y];
    }
}
