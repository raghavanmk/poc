using Microsoft.Extensions.Logging;
using Moq;
using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline.Tests;
public class InferenceRuleTests
{
    private readonly InferenceFilter inferenceFilter;
    private readonly ModelConfig modelConfig;
    public InferenceRuleTests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<InferenceFilter>>();

        modelConfig = new ModelConfig
        {
            LabelMap = new Dictionary<int, string>
            {
                [0] = "jacket",
                [1] = "no-jacket",
                [2] = "helmet",
                [3] = "no-helmet",
                [4] = "drum-on-pallet",
                [5] = "drum-on-drum",
                [6] = "drum-on-floor",
                [7] = "drum-on-floor"
            },
            Camera = new Dictionary<string, CameraDetails>
            {
                ["Q2UV-N5GT-HURS"] = new CameraDetails { Location = "Rail Dock West", Class = [1, 3] },
                ["Q2UV-5LPF-HURS"] = new CameraDetails { Location = "Depack Front Door CMB", Class = [1, 3] },
                ["Q2UV-QYDA-Z3CF"] = new CameraDetails { Location = "Rail Dock East", Class = [1, 3] },
                ["Q2UV-9LPF-KURS"] = new CameraDetails { Location = "Dummyy", Class = [1, 3] },

            },
            ModelInference = new Dictionary<string, ModelInference>
            {
                ["Shared"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Deferred = false
                },
                ["drum-on-floor"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Deferred = true
                }
            },
            CameraFilter = new Dictionary<string, CameraFilter>
            {
                ["Shared"] = new CameraFilter
                {
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["Q2UV-N5GT-HURS"] = new CameraFilter
                {
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["Q2UV-5LPF-HURS"] = new CameraFilter
                {
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["Q2UV-QYDA-Z3CF"] = new CameraFilter
                {
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["Q2UV-9LPF-KURS"] = new CameraFilter
                {
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                }
            }
        };

        inferenceFilter = new InferenceFilter(modelConfig, mockLogger.Object);
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
    public void IfCoordinatesNotProcessedDeferred_TestAllConditions()
    {
        foreach (var (coordinates, timeStamp, camSerial, classId, confidence, expected) in TestDataGenerator.GenerateTestDataFor_IfCoordinatesNotProcessedDeferred())
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

