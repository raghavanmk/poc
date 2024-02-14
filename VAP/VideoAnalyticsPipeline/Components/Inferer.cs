using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;
internal class Inferer(ChannelFactory channelFactory, ILogger<Inferer> logger, InferenceRules infererRules) : IModule
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var data in channelFactory.Reader(nameof(Inferer)).ReadAllAsync(cancellationToken))
        {
            try
            {
                if (!infererRules.TryDetectViolation(data, out var violations)) continue;

                logger.LogInformation("Violation detected for {camSerial} at {timestamp}", data.CameraSerial, data.Inference?.Timestamp);
                data.ViolationDetected = true;
                data.Inference!.Outputs = violations;

                foreach (var writer in channelFactory.Writers(nameof(Inferer)))
                {
                    await writer.WriteAsync(data, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inferring violations for camera {CameraSerial} at {TimeStamp}. Message {Message}",
                    data!.CameraSerial, data.Inference!.Timestamp, data!.Inference!.ToString());
            }
        }
    }
}

