﻿using Microsoft.Extensions.Logging;
using Moq;
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
            Models = new Dictionary<string, ModelInference>
            {
                ["Shared"] = new ModelInference
                {
                    Confidence = 0.7f,
                    Class = [1, 2],
                    Deferred = false,
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
    public void IfCoordinatesNotProcessed_TestAllConditionsInDeferred()
    {
        modelConfig.Models!["Shared"].Deferred = true;

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

