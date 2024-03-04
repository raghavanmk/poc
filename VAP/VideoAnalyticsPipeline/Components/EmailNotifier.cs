using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;
internal class EmailNotifier(ChannelFactory channelFactory, MailManager mailManager, ILogger<EmailNotifier> logger) : IModule
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(EmailNotifier).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                var image = (Image)data;

                if (image != null)
                    await mailManager.SendMail(image.ImageStream!, data.Inference!.ToString(), image.CameraSerial!, image.Inference!.Timestamp, cancellationToken);
                else
                    logger.LogError("Inferred image not available to send email for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error sending email for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp); 
            }
        }
    }
}
