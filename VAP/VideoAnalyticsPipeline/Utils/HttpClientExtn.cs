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
        var bufferInterval = Convert.ToInt16(configuration["Polly:BufferInterval"]);
        
        var timeout = CalculateTimeout(retryCount, retryInterval, bufferInterval);

        services.AddHttpClient("HttpClientWithRetry",client => client.Timeout = TimeSpan.FromSeconds(timeout))
                .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                .WaitAndRetryAsync(retryCount,
                     retry => TimeSpan.FromSeconds(retry * retryInterval),
                     onRetry: (outcome, timespan, retryAttempt, context) =>
                     {
                         logger.Warning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                     }));

        return services;
    }

    // calculate timeout based on retry count and interval.it follows arithmetic progression + 100s buffer
    internal static int CalculateTimeout(int retryCount, int retryInterval, int bufferInterval) =>
        (retryCount * (retryInterval * 2 + (retryCount - 1) * retryInterval)) / 2 + bufferInterval;

}