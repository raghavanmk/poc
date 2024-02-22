namespace VideoAnalyticsPipeline;
public interface IModule
{
    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}
