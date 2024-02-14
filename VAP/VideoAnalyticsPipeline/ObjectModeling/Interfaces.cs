namespace VideoAnalyticsPipeline;
public interface IModule
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
