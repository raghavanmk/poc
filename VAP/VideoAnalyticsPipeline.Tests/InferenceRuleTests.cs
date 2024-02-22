using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline.Tests;
public class InferenceRuleTests
{
    private readonly InferenceFilter inferenceFilter;

    public InferenceRuleTests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<InferenceFilter>>();

        var mockConfiguration = new Mock<IConfiguration>();

        mockConfiguration.Setup(a => a.GetSection("FilteringRules:Timeout"))
                         .Returns(MockConfigurationSection("1000").Object);

        mockConfiguration.Setup(a => a.GetSection("FilteringRules:RadiusLimit"))
                         .Returns(MockConfigurationSection("0.3").Object);

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

        inferenceFilter = new InferenceFilter(modelConfig, mockConfiguration.Object, mockLogger.Object);
    }

    private static Mock<IConfigurationSection> MockConfigurationSection(string value)
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.SetupGet(m => m.Value).Returns(value);
        return mockSection;
    }

    [Fact]
    public void IfCoordinatesNotProcessed_TestAllConditions()
    {

        foreach (var (coordinates, timeStamp, camSerial, classId, confidence, expected) in TestDataGenerator.GenerateTestDataFor_IfCoordinatesNotProcessed())
        {
            // Act
            var result = inferenceFilter.IfCoordinatesNotProcessed(coordinates, timeStamp, camSerial, classId, confidence);

            // Assert
            Assert.Equal(expected, result);

        }
    }

    [Fact]
    public void IfCoordinatesNotNeighbours_TestAllConditions()
    {
        foreach (var (cameraSerial, output, timestamp, expected) in TestDataGenerator.GenerateTestDataFor_IfCoordinatesNotNeighbours())
        {
            //Act
            var result = inferenceFilter.IfCoordinatesNotNeighbours(output, cameraSerial, timestamp);

            //Assert
            Assert.Equal(expected, result);
        }
    }
}

