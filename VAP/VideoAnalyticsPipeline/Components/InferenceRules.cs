using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline;

internal class InferenceRules(ModelConfig modelConfig, InferenceFilter inferenceFilter)
{
    internal bool TryDetectViolation(Data data, out Output[] violations)
    {
        var modelInference = modelConfig[data.CameraSerial!];

        violations = data.Inference?.Outputs?.Where(o => IsValidOutput(o, data) &&
                                                         IfInferenceOutsideThreshold(modelInference, o)).ToArray() ?? [];
        return violations.Length > 0;
    }

    private bool IsValidOutput(Output output, Data data) =>

        inferenceFilter.IfCoordinatesNotOutOfBounds(output.Location!) &&
        inferenceFilter.IfCoordinatesNotProcessed(output.Location!, data.Inference!.Timestamp, data.CameraSerial!, output.Class, output.Score) &&
        inferenceFilter.IfCoordinatesNotNeighbours(output, data.CameraSerial!, data.Inference.Timestamp);    

    internal static bool IfInferenceOutsideThreshold(ModelInference modelInference, Output output) =>

        modelInference!.Class!.Contains(output.Class) &&
               output.Score > modelInference.Confidence;

}
