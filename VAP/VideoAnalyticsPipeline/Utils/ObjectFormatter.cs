using System.Formats.Asn1;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoAnalyticsPipeline;
public class ObjectFormatter
{
    static readonly JsonSerializerOptions serializerOptions =
        new() { PropertyNameCaseInsensitive = true, Converters = { new RoundFloatArrayConverter() } };
    public static async ValueTask<T?> DeserializeAsync<T>(byte[] message, CancellationToken cancellationToken) =>
         await JsonSerializer.DeserializeAsync<T>(new MemoryStream(message), serializerOptions, cancellationToken);

    public static T? Deserialize<T>(string message) =>
          JsonSerializer.Deserialize<T>(message, serializerOptions);
}

public class RoundFloatArrayConverter : JsonConverter<float[]>
{
    public override float[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)        
            throw new JsonException();        
        
        var values = new float[4];
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)            
                return values;            

            float value = reader.GetSingle();
            values[index++] = (float)Math.Round(value, 2);
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, float[] values, JsonSerializerOptions options)
    => JsonSerializer.Serialize(writer, values, options);

}


