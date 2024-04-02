using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;
internal class Inferer(ChannelFactory channelFactory, ILogger<Inferer> logger, InferenceRules infererRules) : IModule
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(Inferer).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                if (!infererRules.TryDetectViolation(data, out var violations)) continue;

                logger.LogInformation("Violation detected in message {message}, inferred from camera {camSerial} at {timestamp}", data.Inference!.ToString(), data.CameraSerial, data.Inference!.Timestamp);
                data.ViolationDetected = true;
                data.Inference!.Outputs = violations;

                foreach (var writer in channelFactory.Writers(currentComponent))
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

