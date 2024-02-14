using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;

internal class InferenceRules(ModelConfig modelConfig, InferenceCache cache, ILogger<InferenceRules> logger)
{ 
    public bool TryDetectViolation(Data data, out Output[] violations)
    {
        var modelInference = modelConfig.ModelInference(data.CameraSerial!);

        violations = data.Inference?.Outputs?.Where(o =>
                        CheckCoordinateBounds(o.Location!)
                        && !cache.TryCheckIfCoordinatesAreProcessed(o.Location!, data.Inference.Timestamp, data.CameraSerial!,o.Class,o.Score)
                        && modelInference!.Class!.Contains(o.Class)
                        && o.Score > modelInference.Confidence).ToArray()
                        ?? [];

        return violations.Length > 0;
    }

    bool CheckCoordinateBounds(float[] coordinates)
    {
        var bounds = coordinates.All(c => c >= 0 && c <= 1);

        if (!bounds)
            logger.LogError("Coordinate bounds {coordinates} are not within 0 and 1. This will not be processed further", coordinates);

        return bounds;
    }
}