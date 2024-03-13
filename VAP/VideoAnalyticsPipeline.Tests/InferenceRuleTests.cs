using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline.Tests;
public class InferenceRuleTests
{
    private readonly InferenceFilter inferenceFilter;
    private ModelConfig modelConfig;
    public InferenceRuleTests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<InferenceFilter>>();

        modelConfig = new ModelConfig
        {
            Cameras = new Dictionary<string, int[]> 
            {
                ["Q2UV-N5GT-HURS"] = [ 1, 3 ],
                ["Q2UV-77ZC-7MVW"] = [ 1 ],
                ["Q2UV-QYDA-Z3CF"] = [ 1, 3 ],
                ["Q2UV-5CWG-EX8A"] = [ 1, 3, 6 ],
                ["Q2UV-6LC2-VKH6"] = [ 1, 3, 6 ],
                ["Q2UV-5LPF-A973"] = [ 1, 3 ],
                ["Q2UW-E263-S2Z4"] = [ 1, 3 ],
                ["Q2UV-PSHP-4KFP"] = [ 1, 3 ],
                ["Q2UV-C86Y-FS7E"] = [ 1, 3 ],
                ["Q2UV-RRD5-7E6F"] = [ 1 ],
                ["Q2UV-ZX7W-JU4K"] = [ 1, 3 ],
                ["Q2UV-AP46-NLJT"] = [ 1 ],
                ["Q2UV-4LFJ-PJ3F"] = [ 1, 3, 6 ],
                ["Q2UV-LA9N-7JRA"] = [ 1, 3, 6 ],
                ["Q2UV-NXTV-RKH4"] = [ 1, 3, 6 ],
                ["Q2UV-XU23-NS3B"] = [ 1, 3, 6 ],
                ["Q2UV-YH9L-UHGW"] = [ 1, 3, 6 ],
            },
            ClassDefaults = new Dictionary<string, ModelInference>
            {
                ["0"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = false,
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["1"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = false,
                    Timeout = 1000, 
                    RadiusLimit = 0.3f
                },
                ["2"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = false,
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["3"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = false,
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["4"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = false,
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["5"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = false,
                    Timeout = 1000,
                    RadiusLimit = 0.3f
                },
                ["6"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = 1,
                    Deferred = true,
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

