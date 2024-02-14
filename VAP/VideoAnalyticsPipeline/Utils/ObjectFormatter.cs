using System.Text.Json;

namespace VideoAnalyticsPipeline;
public class ObjectFormatter
{
    static readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };
    public static async ValueTask<T?> DeserializeAsync<T>(byte[] message, CancellationToken cancellationToken) =>
         await JsonSerializer.DeserializeAsync<T>(new MemoryStream(message), serializerOptions, cancellationToken);

    public static T? Deserialize<T>(string message) =>
          JsonSerializer.Deserialize<T>(message, serializerOptions);

}