using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace VideoAnalyticsPipeline.Tests;
public class InferenceRulesTests
{
    private readonly InferenceRules inferenceRules;
    public InferenceRulesTests()
    {
        // Arrange
        var mockTimeoutConfigurationSection = new Mock<IConfigurationSection>();
        mockTimeoutConfigurationSection.SetupGet(m => m.Value).Returns("180000");

        var mockRadiusConfigurationSection = new Mock<IConfigurationSection>();
        mockRadiusConfigurationSection.SetupGet(m => m.Value).Returns("0.2");

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(a => a.GetSection("InferenceCache:RadiusLimit"))
                         .Returns(mockRadiusConfigurationSection.Object);
        mockConfiguration.Setup(a => a.GetSection("InferenceCache:Timeout"))
                         .Returns(mockTimeoutConfigurationSection.Object);
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

        var mockLogger = new Mock<ILogger<InferenceRules>>();
        var cacheMockLogger = new Mock<ILogger<InferenceCache>>();

        var inferenceCache = new InferenceCache(mockConfiguration.Object, cacheMockLogger.Object, modelConfig);

        inferenceRules = new InferenceRules(modelConfig, inferenceCache, mockLogger.Object, mockConfiguration.Object);
    }

    [Fact]
    public void CheckIfNeighborsAreSimililar_Tests()
    {
        //Arrange
        var testData = new List<(string, Output, long, bool)>
         {
             new ("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 1, Location = new[] { 0.24f, 0.12f, 0.46f, 0.31f }, Score = 8.75f }, 1706679450030, false),
             new ("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 1, Location = new[] { 0.24f, 0.12f, 0.46f, 0.31f }, Score = 8.75f }, 1706679550030, true),
             new ("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 1, Location = new[] { 0.24f, 0.12f, 0.46f, 0.31f }, Score = 8.75f }, 1706679640030, false),
             new ("Q2UV-9LPF-KURS", new Output { Class = 1, Id = 3, Location = new[] { 0.36f, 0.27f, 0.58f, 0.46f }, Score = 8.75f }, 1706679480030, false),
             new ("Q2UV-5LPF-HURS", new Output { Class = 4, Id = 4, Location = new[] { 0.41f, 0.28f, 0.62f, 0.47f }, Score = 8.75f }, 1706679660030, false),
             new ("Q2UV-5LPF-HURS", new Output { Class = 1, Id = 5, Location = new[] { 0.31f, 0.18f, 0.52f, 0.37f }, Score = 8.75f }, 1706679680030, true),
             new ("Q2UV-9LPF-KURS", new Output { Class = 1, Id = 6, Location = new[] { 0.46f, 0.23f, 0.68f, 0.51f }, Score = 8.75f }, 1706679500030, true),
         };

        foreach (var (cameraSerial, output, timestamp, isProcessed) in testData)
        {
            //Act
            var result = inferenceRules.CheckIfNeighborsAreSimilar(output, cameraSerial, timestamp);

            //Assert
            Assert.Equal(isProcessed, result);
        }
    }

}
