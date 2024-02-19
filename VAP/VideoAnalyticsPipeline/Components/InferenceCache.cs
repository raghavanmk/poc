using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace VideoAnalyticsPipeline;
public class InferenceCache(IConfiguration configuration, ILogger<InferenceCache> logger, ModelConfig modelConfig)
{
    private readonly ConcurrentDictionary<string, long> _cache = [];

    // To reduce load on pipeline we filter coordinates that are already processed within a time interval
    // The following rules are applied to check if the coordinates are already processed
    // A key is generated using the coordinates,camera serial, class id and confidence
    // Confidence is converted to a range - low if it is less than the model confidence, high otherwise
    // The key is then added to a dictionary with the timeStamp as the value
    // When a new set of coordinates is received, the key is checked in the dictionary
    // If the key is found and the timeStamp is within the interval, the coordinates are considered processed
    // If the key is found and the timeStamp is outside the interval, the coordinates are considered not processed and timestamp is updated

    public bool TryCheckIfCoordinatesAreProcessed(float[] coordinates, long timeStamp, string camSerial, int classId, float confidence)
    {
        var key = GenerateKey(coordinates, camSerial, classId, confidence);
        if (_cache.TryGetValue(key, out var cachedTimestamp))
        {
            if (timeStamp - cachedTimestamp > configuration.GetValue<int>("InferenceCache:Timeout"))
            {
                _cache[key] = timeStamp;
                return false;
            }
            logger.LogWarning("Coordinates {coordinates} for camera {camSerial} for class {classId} with confidence {confidence} already processed at {cachedTimestamp}",
                coordinates, camSerial, classId, confidence, cachedTimestamp);
            return true;
        }
        else
        {
            if (_cache.TryAdd(key, timeStamp))
                return false;
            throw new InvalidOperationException("Failed to add coordinates to cache");
        }
    }

    string GenerateKey(float[] coordinates, string camSerial, int classId, float confidence)
    {
        var confidenceRange = confidence < modelConfig.ModelConfidence(camSerial) ? "low" : "high";
        var key = string.Join(',', coordinates) + "," + camSerial + "," + classId + "," + confidenceRange;
        return key;
    }
}
