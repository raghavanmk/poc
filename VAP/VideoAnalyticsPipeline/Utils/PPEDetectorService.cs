using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;
internal class PPEDetectorService(IEnumerable<IModule> modules,ILogger<PPEDetectorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tasks = modules.Select(module => module.ExecuteAsync(cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.Flatten().InnerExceptions)
            {
                logger.LogError(ex, "Error executing modules");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing modules");
        }
    }
}
