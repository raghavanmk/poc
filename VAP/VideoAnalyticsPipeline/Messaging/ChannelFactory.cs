using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace VideoAnalyticsPipeline;
public class ChannelFactory
{
    readonly ConcurrentDictionary<string, List<ChannelWriter<Data>>> writers = [];
    readonly ConcurrentDictionary<string, ChannelReader<Data>> readers = [];
    public ChannelFactory(IConfiguration configuration)
    {
        var pipeline = configuration
                        .GetSection("Pipeline")
                        .Get<Dictionary<string, string[]>>()
                        ?? throw new KeyNotFoundException("No pipeline found in configuration");

        foreach (var kvp in pipeline)
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
    public List<ChannelWriter<Data>> Writers(string key)
    {
        if (writers.TryGetValue(key, out var writerList))
        {
            return writerList;
        }

        throw new KeyNotFoundException($"No writers found for key {key}");
    }

    public ChannelReader<Data> Reader(string key)
    {
        if (readers.TryGetValue(key, out var reader))
        {
            return reader;
        }

        throw new KeyNotFoundException($"No reader found for key {key}");
    }
}

public class ChannelManager
{
    public static Channel<T> Create<T>() => Channel.CreateUnbounded<T>();
}