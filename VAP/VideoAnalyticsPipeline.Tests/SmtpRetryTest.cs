using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Serilog;
using System.Net.Mail;

namespace VideoAnalyticsPipeline.Tests;
public class SmtpRetryTest
{
    [Fact]
    public void AddSmtpRetryPolicy_ShouldAddPolicyToServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var logger = new LoggerConfiguration().CreateLogger();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Polly:SmtpRetryPolicy:RetryCount", "3" },
                { "Polly:SmtpRetryPolicy:RetryInterval", "1" }
            }!)
            .Build();

        // Act
        ResiliencyPolicy.AddSmtpRetryPolicy(services, logger, configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var policy = serviceProvider.GetService<IAsyncPolicy>();
        Assert.NotNull(policy);
    }

    [Fact]
    public async Task SendMail_ShouldRetry_WhenSmtpExceptionIsThrown()
    {
        // Arrange
        var smtpClientMock = new Mock<ISmtpClient>();

        smtpClientMock
            .Setup(client => client.SendMailAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpException());

        var retryCount = 3;
        var retryInterval = TimeSpan.FromSeconds(1);

        var smtpRetryPolicy = Policy
            .Handle<SmtpException>()
            .WaitAndRetryAsync(retryCount, _ => retryInterval);

        var mailManager = new MailManager(new Mock<ILogger<MailManager>>().Object,
                                          smtpClientMock.Object,
                                          smtpRetryPolicy);

        // Act
        await mailManager.SendMail("test@test.com", "", ["test@test.com"], "", "", Stream.Null, "", "image/jpeg", CancellationToken.None);

        // Assert
        smtpClientMock.Verify(
            client => client.SendMailAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(retryCount + 1));
    }

}
