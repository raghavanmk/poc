using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace VideoAnalyticsPipeline;

internal class MailManager
{
    private readonly SmtpClient smtpClient;
    private readonly string subject;
    private readonly string body;
    private readonly string[]? emails;
    private readonly ILogger<MailManager> logger;
    private readonly MailAddress fromAddress;

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

        subject = configuration["Notification:Subject"] ?? "Violation Detected";
        body = configuration["Notification:Message"] ?? "Violation Detected";
        emails = configuration.GetSection("Notification:Email").Get<string[]>();

        this.logger = logger;
    }
    
    internal async ValueTask SendMail(Stream imageStream, string infMessage, string camSerial, long timestamp, string camName, string labels, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var email in emails ?? Enumerable.Empty<string>())
            {
                logger.LogInformation("Sending email to {email}", email);

                var toAddress = new MailAddress(email);
                
                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = string.Format(subject,camSerial,camName),
                    Body = string.Format(body, camSerial, camName, DateTimeOffset.FromUnixTimeMilliseconds(timestamp), labels, infMessage),
                    IsBodyHtml = true
                };

                imageStream.Position = 0;
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                var attachment = new Attachment(memoryStream, $"{camSerial}_{timestamp}.jpeg", "image/jpeg");
                message.Attachments.Add(attachment);

                await smtpClient.SendMailAsync(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email");
        }
    }
}

