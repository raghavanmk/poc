using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;
internal class EmailNotifier(ChannelFactory channelFactory, MailManager mailManager, ILogger<EmailNotifier> logger) : IModule
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var data in channelFactory.Reader(nameof(EmailNotifier)).ReadAllAsync(cancellationToken))
        {
            var image = (Image)data;

            if (image != null)
                await mailManager.SendMail(image.ImageStream!, data.Inference!.ToString(), image.CameraSerial!, image.Inference!.Timestamp, cancellationToken);
            else
                logger.LogError("Inferred image not available to send email for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
        }
    }
}
