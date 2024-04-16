using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline;

internal class InferenceRules(ModelConfig modelConfig, InferenceFilter inferenceFilter)
{
    internal bool TryDetectViolation(Data data, out Output[] violations, out bool confinedSpace)
    {
        var classInference = modelConfig[data.CameraSerial!];

        var cameraRules = modelConfig.CameraRules(data.CameraSerial!);

        violations = data.Inference!.Outputs!.Where(o => IfInferenceOutsideThreshold(classInference, o) &&
                                                         FilterInferences(o, data)).ToArray() ?? [];

        if (cameraRules.Contains("ConfinedSpace"))
        {
            confinedSpace = CountCheck(data, out Output[] outputs);
            if (violations.Length == 0 && confinedSpace) violations = outputs;
        }
        else confinedSpace = false;

        return violations.Length > 0;
    }

    private bool FilterInferences(Output output, Data data) =>

        inferenceFilter.IfCoordinatesNotOutOfBounds(output.Location!) &&
        inferenceFilter.IfCoordinatesNotProcessed(output.Location!, data.Inference!.Timestamp, data.CameraSerial!, output.Class, output.Score) &&
        inferenceFilter.IfCoordinatesNotNeighbours(output, data.CameraSerial!, data.Inference.Timestamp);    

    internal bool IfInferenceOutsideThreshold(int[] classInference, Output output) =>

        classInference!.Contains(output.Class) &&
               output.Score > modelConfig.ModelConfidence(output.Class);

    internal bool CountCheck(Data data, out Output[] outputs)
    {
        var classInference = modelConfig.CountClasses(data.CameraSerial!);

        outputs = data.Inference!.Outputs!.Where(o => IfInferenceOutsideThreshold(classInference, o)).ToArray();

        var countCheck = inferenceFilter.IfCountIsNotCorrect(outputs, data.CameraSerial!, data.Inference.Timestamp);

        return countCheck;
    }
}
