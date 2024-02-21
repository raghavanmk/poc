using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog.Core;

namespace VideoAnalyticsPipeline;
internal static class HttpClientExtn
{
    internal static IServiceCollection AddHttpClientPolicy(this IServiceCollection services, Logger logger, IConfiguration configuration)
    {        
        var retryCount = Convert.ToInt16(configuration["Polly:RetryCount"]);
        var retryInterval = Convert.ToInt16(configuration["Polly:RetryInterval"]);

        services.AddHttpClient("HttpClientWithRetry")
                 .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                 .WaitAndRetryAsync(retryCount,
                     retry => TimeSpan.FromSeconds(retry * retryInterval),
                     onRetry: (outcome, timespan, retryAttempt, context) =>
                     {
                         logger.Warning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                     }));

        return services;
    }
}