using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog.Core;
using System.Net.Mail;

namespace VideoAnalyticsPipeline;
internal static class ResiliencyPolicy
{
    internal static IServiceCollection AddHttpClientPolicy(this IServiceCollection services, Logger logger, IConfiguration configuration)
    {
        var retryCount = Convert.ToInt16(configuration["Polly:HttpClientRetryPolicy:RetryCount"]);
        var retryInterval = Convert.ToInt16(configuration["Polly:HttpClientRetryPolicy:RetryInterval"]);
        var bufferInterval = Convert.ToInt16(configuration["Polly:HttpClientRetryPolicy:BufferInterval"]);

        var timeout = CalculateTimeout(retryCount, retryInterval, bufferInterval);

        services.AddHttpClient("HttpClientWithRetry", client => client.Timeout = TimeSpan.FromSeconds(timeout))
                .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                .WaitAndRetryAsync(retryCount,
                     retry => TimeSpan.FromSeconds(retry * retryInterval),
                     onRetry: (outcome, timespan, retryAttempt, context) =>
                     {
                         logger.Warning($"Delaying API call for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                     }));

        return services;
    }

    internal static IServiceCollection AddSmtpRetryPolicy(this IServiceCollection services, Logger logger, IConfiguration configuration)
    {
        var retryCount = Convert.ToInt16(configuration["Polly:SmtpRetryPolicy:RetryCount"]);
        var retryInterval = Convert.ToInt16(configuration["Polly:SmtpRetryPolicy:RetryInterval"]);

        var smtpPolicy = (IAsyncPolicy)Policy
                        .Handle<SmtpException>()
                        .WaitAndRetryAsync(retryCount,
                            retry => TimeSpan.FromSeconds(retry * retryInterval),
                            onRetry: (outcome, timespan, retryAttempt, context) =>
                            {
                                logger.Warning($"Delaying sending Email for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                            });

        services.AddSingleton(smtpPolicy);

        return services;
    }

    // calculate timeout based on retry count and interval.it follows arithmetic progression + buffer added to complete last retry
    internal static int CalculateTimeout(int retryCount, int retryInterval, int bufferInterval = 0) =>
        (retryCount * (retryInterval * 2 + (retryCount - 1) * retryInterval)) / 2 + bufferInterval;

}