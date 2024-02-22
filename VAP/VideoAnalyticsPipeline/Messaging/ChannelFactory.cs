using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace VideoAnalyticsPipeline;

internal class ChannelFactory
{
    private readonly ConcurrentDictionary<string, List<ChannelWriter<Data>>> writers = [];
    private readonly ConcurrentDictionary<string, ChannelReader<Data>> readers = [];
    public ChannelFactory(PipelineComponentsConfig pipelineComponentsConfig)
    {
        foreach (var kvp in pipelineComponentsConfig.PipelineComponents!)
        {
            writers.TryAdd(kvp.Key, []);

            foreach (var item in kvp.Value)
            {
                var writer = ChannelManager.Create<Data>();
                writers[kvp.Key].Add(writer);
                readers.TryAdd(item, writer.Reader);
            }
        }
    }
    internal List<ChannelWriter<Data>> Writers(string key)
    {
        if (writers.TryGetValue(key, out var writerList))
        {
            return writerList;
        }

        throw new KeyNotFoundException($"No writers found for key {key}");
    }

    internal ChannelReader<Data> Reader(string key)
    {
        if (readers.TryGetValue(key, out var reader))
        {
            return reader;
        }

        throw new KeyNotFoundException($"No reader found for key {key}");
    }
}

internal class ChannelManager
{
    internal static Channel<T> Create<T>() => Channel.CreateUnbounded<T>();
}