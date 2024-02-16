using Microsoft.Extensions.Logging;
using KdTree;
using KdTree.Math;
using Microsoft.Extensions.Configuration;

namespace VideoAnalyticsPipeline;

public class InferenceRules(ModelConfig modelConfig, InferenceCache cache, ILogger<InferenceRules> logger, IConfiguration configuration, IDictionary<string, KdTree<float, Detection>> violationTree)
{ 
    public bool TryDetectViolation(Data data, out Output[] violations)
    {
        var modelInference = modelConfig.ModelInference(data.CameraSerial!);

        violations = data.Inference?.Outputs?.Where(o =>
                        CheckCoordinateBounds(o.Location!)
                        && !cache.TryCheckIfCoordinatesAreProcessed(o.Location!, data.Inference.Timestamp, data.CameraSerial!,o.Class,o.Score)
                        && modelInference!.Class!.Contains(o.Class)
                        && o.Score > modelInference.Confidence
                        && checkViolation(o, data.CameraSerial!, data.Inference.Timestamp)).ToArray()
                        ?? [];

        return violations.Length > 0;
    }
    public bool checkViolation(Output output, string cameraSerial, long timeStamp)
    {
        var key = GenerateKey(output.Location!, cameraSerial);

        if (!violationTree.TryGetValue(key, out var tree))
        {
            tree = new KdTree<float, Detection>(2, new FloatMath());
            violationTree[key] = tree;
        }

        var centre = RectCentre(output.Location!);

        var allneighbors = tree.RadialSearch(centre, configuration.GetValue<float>("InferenceCache:RadiusLimit")).ToArray(); 

        var filteredNeighbors = allneighbors.Length > 0 ? filterNeighbors(allneighbors, cameraSerial!) : [];

        var detection = new Detection
        {
            Output = output,
            CameraSerial = cameraSerial,
            Timestamp = timeStamp
        };
     
        if ((tree.Any() && timeStamp - tree.Max(n => n.Value.Timestamp) < configuration.GetValue<int>("InferenceCache:Timeout")) ||
             filteredNeighbors.Any(n => timeStamp - n.Value.Timestamp < configuration.GetValue<int>("InferenceCache:Timeout")))
        {
            tree.Add(centre, detection);
            return false;
        }

        tree.Add(centre, detection);
        return true;
    }

    bool CheckCoordinateBounds(float[] coordinates)
    {
        var bounds = coordinates.All(c => c >= 0 && c <= 1);

        if (!bounds)
            logger.LogError("Coordinate bounds {coordinates} are not within 0 and 1. This will not be processed further", coordinates);

        return bounds;
    }

    string GenerateKey(float[] coordinates, string camSerial)
    {
        return string.Join(",", coordinates) + "," + camSerial;
    }

    public KdTreeNode<float, Detection>[] filterNeighbors(KdTreeNode<float, Detection>[] neighbors, string camSerial)
    {
        List<KdTreeNode<float, Detection>> filtered = new List<KdTreeNode<float, Detection>>();
        foreach (var neighbor in neighbors)
        {
            if (neighbor.Value.CameraSerial == camSerial) filtered.Add(neighbor);
        }
        return filtered.ToArray();
    }

    float[] RectCentre(float[] coordinates)
    {
        var x = (coordinates[0] + coordinates[2])/2;
        var y = (coordinates[1] + coordinates[3])/2;

        return [x, y];   
    }
}
