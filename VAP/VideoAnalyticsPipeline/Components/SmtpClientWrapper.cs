using System.Net.Mail;

namespace VideoAnalyticsPipeline.Components;
internal class SmtpClientWrapper(SmtpClient smtpClient) : ISmtpClient
{
    public async ValueTask SendMailAsync(MailMessage mailMessage, CancellationToken cancellationToken) =>
        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
}
