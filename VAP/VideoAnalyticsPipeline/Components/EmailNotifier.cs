using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace VideoAnalyticsPipeline;
internal class EmailNotifier(IConfiguration configuration, ChannelFactory channelFactory, MailManager mailManager, ILogger<EmailNotifier> logger) : IModule
{
    private readonly string[]? emails = configuration.GetSection("Notification:Email").Get<string[]>();
    private readonly string subject = configuration["Notification:Subject"] ?? "Violation Detected";
    private readonly string body = configuration["Notification:Message"] ?? "Violation Detected";
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(EmailNotifier).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                var image = (Image)data;

                var cameraName = configuration[$"Camera:{image.CameraSerial}:Location"]!;

                var classes = data.Inference!.Outputs!.Select(x => x.Class);

                var labels = GetLabels(classes);

                if (image != null)
                    await SendEmail(image.CameraSerial!, cameraName, image.Inference!.Timestamp, labels, data.Inference!.ToString(), image.ImageStream!, cancellationToken);
                else
                    logger.LogError("Inferred image not available toAddress send toAddress for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending toAddress for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
        }
    }

    private string GetLabels(IEnumerable<int> classes)
    {
        var labelsBuilder = new StringBuilder();

        foreach (var cls in classes.Distinct())
        {
            labelsBuilder.AppendLine(configuration[$"LabelMap:{cls}"]);
        }
        labelsBuilder.Length -= 2;
        return labelsBuilder.ToString();

    }

    private async ValueTask SendEmail(string camSerial, string camName, long timeStamp, string labels, string infMessage,
       Stream image, CancellationToken cancellationToken)
    {
        var fromAddress = configuration["SMTP:Address"]!;
        var displayName = configuration["SMTP:DisplayName"]!;
        var mailSubject = string.Format(subject, camSerial, camName);
        var mailBody = string.Format(body, camSerial, camName, DateTimeOffset.FromUnixTimeMilliseconds(timeStamp), labels, infMessage);
        var attachmentName = $"{camSerial}_{timeStamp}.jpeg";
        var mediaType = "image/jpeg";
        var toAddresses = emails ?? Array.Empty<string>();
        await mailManager.SendMail(fromAddress, displayName, toAddresses, mailSubject, mailBody, image, attachmentName, mediaType, cancellationToken);
        logger.LogInformation("Sent email to {toAddresses}", string.Join(",",toAddresses));
    }
}
