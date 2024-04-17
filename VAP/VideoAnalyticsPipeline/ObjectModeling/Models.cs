namespace VideoAnalyticsPipeline;

public class Data
{
    public Inference? Inference { get; set; }
    public bool ViolationDetected { get; set; }
    public string? CameraSerial { get; set; }
    public bool ConfinedSpace { get; set; }
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
    // location values contain normalized coordinates of the bounding box. it follows [xmin, ymin, xmax, ymax] format
    public float[]? Location { get; set; }
    public float Score { get; set; }
}

public class Image : Data
{
    public Stream? ImageStream { get; set; }

}

public class ModelConfig
{
    public Dictionary<string, ModelInference>? ModelInference { get; set; }
    public Dictionary<string, CameraDetails>? Camera { get; set; }
    public Dictionary<int, string>? LabelMap { get; set; }
    public Dictionary<string, CameraFilter>? CameraFilter { get; set; }
    public Dictionary<string, string[]>? EmailAlertGroup { get; set; }
    public Dictionary<string, string[]> Emails { get; private set; }
    public Dictionary<string, string[]> CameraRule { get; set; }


    public ModelConfig()
    {
        Emails = [];
        CameraRule = [];
    }
    public ModelInference this[int classId]
    {
        get
        {
            if (ModelInference!.TryGetValue(LabelMap![classId], out var modelInference))
                return modelInference!;

            return ModelInference["Shared"]!;
        }
    }

    public int[] this[string cameraSerial]
    {
        get
        {
            if (Camera!.TryGetValue(cameraSerial, out var camera))
                return camera.Class!;

            return Camera!["Shared"].Class!;
        }
    }
    public float ModelConfidence(int classId)
    {
        if (ModelInference!.TryGetValue(LabelMap![classId], out var modelInference))
            return modelInference.Confidence;

        return ModelInference["Shared"].Confidence;
    }

    public float RadiusLimit(string cameraSerial)
    {
        if (CameraFilter!.TryGetValue(cameraSerial, out var cameraFilter))
            return cameraFilter.RadiusLimit;

        return CameraFilter!["Shared"].RadiusLimit;
    }

    public long Timeout(string cameraSerial)
    {
        if (CameraFilter!.TryGetValue(cameraSerial, out var cameraFilter))
            return cameraFilter.Timeout;

        return CameraFilter!["Shared"].Timeout;
    }

    public void ConfigEmailAlerts()
    {
        var adminEmails = EmailAlertGroup!["Admins"];
        foreach (var camera in Camera!)
        {
            var emailAlertGroups = camera.Value.EmailAlertGroup ?? [];
            var emails = new List<string>(adminEmails);

            foreach (var emailAlertGroup in emailAlertGroups)
            {
                if (EmailAlertGroup!.TryGetValue(emailAlertGroup, out var emailList)
                    && emailList is not null)
                {
                    emails.AddRange(emailList);
                }
            }

            Emails[camera.Key] = emails.Distinct().ToArray();
        }
    }
    public int[] CountClasses(string cameraSerial)
    {
        if (Camera!.TryGetValue(cameraSerial, out var camera))
            return camera.CountClass!;

        return Camera!["Shared"].Class!;
    }
    public long? CountTimeout(string cameraSerial)
    {
        if (CameraFilter!.TryGetValue(cameraSerial, out var cameraFilter))
            return cameraFilter.CountTimeout;

        return CameraFilter!["Shared"].CountTimeout;
    }

    public int? Count(string cameraSerial)
    {
        if (CameraFilter!.TryGetValue(cameraSerial, out var cameraFilter))
            return cameraFilter.Count;

        return CameraFilter!["Shared"].Count;
    }

    public string[] CameraRules(string cameraSerial)
    {
        if (CameraRule!.TryGetValue(cameraSerial, out var cameraRule))
            return cameraRule;

        return CameraRule!["Shared"];
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
    public float Confidence { get; set; }
    public bool Deferred { get; set; }
}

public class Detection
{
    public Output? Output { get; set; }
    public long Timestamp { get; set; }
}

public class CameraDetails
{
    public int[]? Class { get; set; }
    public string? Location { get; set; }
    public string[]? EmailAlertGroup { get; set; }
    public int[]? CountClass { get; set; }

}

public class CameraFilter
{
    public long Timeout { get; set; }
    public float RadiusLimit { get; set; }
    public long? CountTimeout { get; set; }
    public int? Count { get; set; }
}