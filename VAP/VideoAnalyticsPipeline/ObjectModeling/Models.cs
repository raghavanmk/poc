
namespace VideoAnalyticsPipeline;

public class Data
{
    public Inference? Inference { get; set; }
    public bool ViolationDetected { get; set; }
    public string? CameraSerial { get; set; }
}
public class Inference
{
    public Output[]? Outputs { get; set; }
    public long Timestamp { get; set; }
    public override string ToString() => MessageFormatter.Serialize(this);
}

public class Output
{
    public int Class { get; set; }
    public int Id { get; set; }
    // location values contain normalized coordinates of the bounding box. it follows [ymin, xmin, ymax, xmax] format
    public float[]? Location { get; set; }
    public float Score { get; set; }
}

public class Image : Data
{
    public Stream? ImageStream { get; set; }

}

public class ModelConfig
{
    public Dictionary<string, ModelInference>? ClassInference { get; set; }
    public Dictionary<string, int[]>? Camera { get; set; }

    public ModelInference this[int classId]
    {
        get
        {
            if(ClassInference!.TryGetValue(classId.ToString(), out var modelInference))
                return modelInference!;

            return ClassInference["Shared"]!;
        }
    }

    public int[] this[string cameraSerial]
    {
        get
        {
            Camera!.TryGetValue(cameraSerial, out var classArr);
            return classArr!;
        }
    }
    public float ModelConfidence(int classId)
    {
        if (ClassInference!.TryGetValue(classId.ToString(), out var modelInference))
            return modelInference.Confidence;

        return ClassInference["Shared"].Confidence;
    }
}

public class PipelineComponentsConfig
{
    public Dictionary<string, string[]>? PipelineComponents { get; set; }

    public HashSet<string> Components
    {
        get
        {
            var pipelineTypes = new HashSet<string>();

            foreach (var pair in PipelineComponents!)
            {
                if (!pipelineTypes.Contains(pair.Key))
                {
                    pipelineTypes.Add(pair.Key);
                }
                foreach (var value in pair.Value)
                {
                    pipelineTypes.Add(value);
                }
            }
            return pipelineTypes;
        }
    }
}
public class ModelInference
{
    public int? Class { get; set; }
    public float Confidence { get; set; }
    public int Timeout { get; set; }
    public bool Deferred { get; set; }
    public float RadiusLimit { get; set; }
}

public class Detection
{
    public Output? Output { get; set; }
    public long Timestamp { get; set; }
}
