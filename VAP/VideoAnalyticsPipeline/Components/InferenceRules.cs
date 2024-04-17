using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline;

internal class InferenceRules(ModelConfig modelConfig, InferenceFilter inferenceFilter)
{
    // todo refactor
    internal bool TryDetectViolation(Data data, out Output[] violations)
    {
        var classInference = modelConfig[data.CameraSerial!];

        var cameraRules = modelConfig.CameraRules(data.CameraSerial!);

        if(cameraRules.Contains("ConfinedSpace"))
        {
            var confinedSpace = CountCheck(data, data.Inference!.Outputs!);

            if(confinedSpace)
            {
                data.ConfinedSpace = true;
                violations = data.Inference!.Outputs!;
                return true;
            }

            violations = data.Inference!.Outputs!.Where(o => IfInferenceOutsideThreshold(classInference, o) && FilterInferences(o, data)).ToArray() ?? [];
        }
        else
        {            
            violations = data.Inference!.Outputs!.Where(o => IfInferenceOutsideThreshold(classInference, o) &&
                                                         FilterInferences(o, data)).ToArray() ?? [];
        }

        return violations.Length > 0;
    }

    private bool FilterInferences(Output output, Data data) =>

        inferenceFilter.IfCoordinatesNotOutOfBounds(output.Location!) &&
        inferenceFilter.IfCoordinatesNotProcessed(output.Location!, data.Inference!.Timestamp, data.CameraSerial!, output.Class, output.Score) &&
        inferenceFilter.IfCoordinatesNotNeighbours(output, data.CameraSerial!, data.Inference.Timestamp);    

    internal bool IfInferenceOutsideThreshold(int[] classInference, Output output) =>

        classInference!.Contains(output.Class) &&
               output.Score > modelConfig.ModelConfidence(output.Class);

    internal bool CountCheck(Data data, Output[] outputs)
    {
        if (outputs.Length < modelConfig.Count(data.CameraSerial!))
        {
            var isProcessed = inferenceFilter.IfCoordinatesForConfinedSpaceIsProcessed(data.CameraSerial!, data.Inference!.Timestamp);

            foreach (var output in outputs)
            {
                output.Class = 50;
            }
            return isProcessed;
        }       

        return false;
    }
}
