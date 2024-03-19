using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline;

internal class InferenceRules(ModelConfig modelConfig, InferenceFilter inferenceFilter)
{
    internal bool TryDetectViolation(Data data, out Output[] violations)
    {
        var classInference = modelConfig[data.CameraSerial!];

        violations = data.Inference!.Outputs!.Where(o => FilterInferences(o, data) &&
                                                         IfInferenceOutsideThreshold(classInference, o)).ToArray() ?? [];
        return violations.Length > 0;
    }

    private bool FilterInferences(Output output, Data data) =>

        inferenceFilter.IfCoordinatesNotOutOfBounds(output.Location!) 
        &&
        (
            inferenceFilter.IfCoordinatesNotProcessed(output.Location!, data.Inference!.Timestamp, data.CameraSerial!, output.Class, output.Score) &
            inferenceFilter.IfCoordinatesNotNeighbours(output, data.CameraSerial!, data.Inference.Timestamp)
        );    

    internal bool IfInferenceOutsideThreshold(int[] classInference, Output output) =>

        classInference!.Contains(output.Class) &&
               output.Score > modelConfig.ModelConfidence(output.Class);

}
