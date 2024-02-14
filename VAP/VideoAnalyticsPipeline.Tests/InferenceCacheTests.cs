using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace VideoAnalyticsPipeline.Tests;
public class InferenceCacheTests
{
    private readonly InferenceCache inferenceCache;

    public InferenceCacheTests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<InferenceCache>>();

        var mockConfigurationSection = new Mock<IConfigurationSection>();
        mockConfigurationSection.SetupGet(m => m.Value).Returns("1000");

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(a => a.GetSection("InferenceCache:Timeout"))
                        .Returns(mockConfigurationSection.Object);

        var modelConfig = new ModelConfig
        {
            Models = new Dictionary<string, ModelInference>
            {
                ["Shared"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = [1, 2]
                }
            }
        };

        inferenceCache = new InferenceCache(mockConfiguration.Object, mockLogger.Object, modelConfig);
    }

    [Fact]
    public void TryCheckIfCoordinatesAreProcessed_TestAllConditions()
    {

        // 1706679450000 1/31/2024 5:37:30 AM +00:00

        // Arrange
        var testData = new List<(float[], long, string, int, float, bool)>
        {
            // first set of coordinates 
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679450000, "Q2UV-N5GT-HURS", 2, 0.9f, false),

            // same coordinates, confidence, class but different camera and within same time interval
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679450005, "Q2UV-5LPF-HURS", 2, 0.9f, false),

            // same coordinates, camera, confidence, class but and same time interval
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679450010, "Q2UV-N5GT-HURS", 2, 0.9f, true),

            // same coordinates, camera, confidence,but different class id and within same time interval
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679450020, "Q2UV-N5GT-HURS", 1, 0.9f, false),

            // same coordinates, camera, class, different confidence within same range and within same time interval
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679450030, "Q2UV-N5GT-HURS", 2, 0.8f, true),

            // same coordinates, camera, class, confidence but different range and within same time interval
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679450040, "Q2UV-N5GT-HURS", 2, 0.6f, false),

            // same coordinates, camera, confidence, class but different timestamp > 1000ms 
            new ([ 0.1f, 0.2f, 0.3f, 0.4f ], 1706679451020, "Q2UV-N5GT-HURS", 2, 0.9f, false)

        };

        foreach (var (coordinates, timeStamp, camSerial, classId, confidence, isProcessed) in testData)
        {
            // Act
            var result = inferenceCache.TryCheckIfCoordinatesAreProcessed(coordinates, timeStamp, camSerial, classId, confidence);

            // Assert
            Assert.Equal(isProcessed, result);

        }
    }
}
