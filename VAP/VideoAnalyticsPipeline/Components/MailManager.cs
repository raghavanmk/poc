using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Mail;

namespace VideoAnalyticsPipeline;

public class MailManager(ILogger<MailManager> logger, ISmtpClient smtpClientWrapper, IAsyncPolicy smtpRetryPolicy)
{
    private readonly bool isBodyHtml = true;

    public async ValueTask SendMail(string fromAddress, string displayName, string[] toAddresses, string subject, string body,
        Stream attachmentStream, string attachmentName, string mediaType, CancellationToken cancellationToken)
    {
        try
        {
            var from = new MailAddress(fromAddress, displayName);

            using var message = new MailMessage
            {
                From = from,
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml
            };

            foreach (var toAddress in toAddresses)
            {
                message.Bcc.Add(toAddress);
            }

            attachmentStream.Position = 0;
            using var memoryStream = new MemoryStream();
            await attachmentStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var attachment = new Attachment(memoryStream, attachmentName, mediaType);
            message.Attachments.Add(attachment);

            await smtpRetryPolicy.ExecuteAsync(
                async (cancellationToken) => await smtpClientWrapper.SendMailAsync(message, cancellationToken),
                cancellationToken);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email");
        }
    }
}

