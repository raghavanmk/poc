using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace VideoAnalyticsPipeline;
internal class EmailNotifier(IConfiguration configuration, ChannelFactory channelFactory, MailManager mailManager, ILogger<EmailNotifier> logger, ModelConfig modelConfig) : IModule
{
    private readonly string subject = configuration["Email:Subject"] ?? "Violation Detected";
    private readonly string body = configuration["Email:Message"] ?? "Violation Detected";
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(EmailNotifier).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                var emails = modelConfig.Emails![data.CameraSerial!];

                if (emails == null || emails.Length == 0)
                {
                    logger.LogWarning("Emails not configured for {camSerial}", data.CameraSerial);
                    continue;
                }

                var image = (Image)data;

                if (image == null)
                {
                    logger.LogError("Inferred image not available toAddress send toAddress for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
                    continue;
                }

                var classes = data.Inference!.Outputs!.Select(x => x.Class);

                var labels = GetLabels(classes);

                await SendEmail(image.CameraSerial!, emails, image.Inference!.Timestamp, labels, data.Inference!.ToString(), image.ImageStream!, cancellationToken);

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

        return labelsBuilder.ToString();

    }

    private async ValueTask SendEmail(string camSerial, string[]? emails, long timeStamp, string labels, string infMessage, Stream image, CancellationToken cancellationToken)
    {
        var cameraName = configuration[$"Camera:{camSerial}:Location"]!;

        var fromAddress = configuration["SMTP:Address"]!;

        var displayName = configuration["SMTP:DisplayName"]!;

        var mailSubject = string.Format(subject, camSerial, cameraName);

        var mailBody = string.Format(body, camSerial, cameraName, DateTimeOffset.FromUnixTimeMilliseconds(timeStamp), labels, infMessage);

        var attachmentName = $"{camSerial}_{timeStamp}.jpeg";

        var mediaType = "image/jpeg";

        var toAddresses = emails ?? [];

        await mailManager.SendMail(fromAddress, displayName, toAddresses, mailSubject, mailBody, image, attachmentName, mediaType, cancellationToken);

        logger.LogInformation("Sent email to {toAddresses}", string.Join(",", toAddresses));
    }
}
