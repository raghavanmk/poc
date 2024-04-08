using System.Net.Mail;

namespace VideoAnalyticsPipeline;
public interface IModule
{
    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}

public interface ISmtpClient
{
    ValueTask SendMailAsync(MailMessage mailMessage, CancellationToken cancellationToken);
}

