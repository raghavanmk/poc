using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace VideoAnalyticsPipeline;

internal class MailManager
{
    private readonly SmtpClient smtpClient;
    private readonly ILogger<MailManager> logger;
    private readonly MailAddress fromAddress;
    private readonly bool isBodyHtml;

    public MailManager(IConfiguration configuration, ILogger<MailManager> logger)
    {
        string fromPassword = configuration["SMTP:Password"]!;
        string smtpHost = configuration["SMTP:Host"]!;
        int smtpPort = Convert.ToInt16(configuration["Notification:Port"]);

        fromAddress = new MailAddress(configuration["SMTP:Address"]!, configuration["SMTP:DisplayName"]!);

        smtpClient = new SmtpClient
        {
            Host = smtpHost,
            Port = smtpPort,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        isBodyHtml = true;
        this.logger = logger;
    }

    internal async ValueTask SendMail(string fromAddress, string displayName, string toAddress, string subject, string body,
        Stream attachmentStream, string? attachmentName, string? mediaType, CancellationToken cancellationToken)
    {
        try
        {
            var from = new MailAddress(fromAddress, displayName);
            var to = new MailAddress(toAddress);

            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml
            };            

            attachmentStream.Position = 0;
            using var memoryStream = new MemoryStream();
            await attachmentStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var attachment = new Attachment(memoryStream, attachmentName, mediaType);
            message.Attachments.Add(attachment);

            await smtpClient.SendMailAsync(message, cancellationToken);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email");
        }
    }
}

