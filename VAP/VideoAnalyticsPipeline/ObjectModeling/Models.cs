using System.Text.Json;

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
    public override string ToString() => JsonSerializer.Serialize(this);
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
    public Dictionary<string, ModelInference>? Models { get; set; }

    public ModelInference ModelInference(string cameraSerial)
    {
        if(Models!.TryGetValue(cameraSerial, out var modelInference))
            return modelInference;
        return Models!["Shared"];
    }
    public float ModelConfidence(string cameraSerial)
    {
        if(Models!.TryGetValue(cameraSerial, out var modelInference))
            return modelInference.Confidence;
        return Models!["Shared"].Confidence;
    }
}

public class ModelInference
{
    public int[]? Class { get; set; }
    public float Confidence { get; set; }
}

public class Detection
{
    public Output? Output { get; set; }
    public string? CameraSerial { get; set; }
    public long Timestamp { get; set; }
}
