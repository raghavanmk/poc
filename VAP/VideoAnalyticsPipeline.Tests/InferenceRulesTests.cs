using KdTree;
using KdTree.Math;
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
        var mockConfigurationSection = new Mock<IConfigurationSection>();
        mockConfigurationSection.SetupGet(m => m.Value).Returns("180000");

        var mockRadiusConfigurationSection = new Mock<IConfigurationSection>();
        mockRadiusConfigurationSection.SetupGet(m => m.Value).Returns("1.0");

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(a => a.GetSection("InferenceCache:Timeout"))
                        .Returns(mockConfigurationSection.Object);
        mockConfiguration.Setup(a => a.GetSection("InferenceCache:RadiusLimit"))
                        .Returns(mockRadiusConfigurationSection.Object);


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

         var violationTreeMock = new Mock<Dictionary<string, KdTree<float, Detection>>>();

         var tree1 = new KdTree<float, Detection>(4, new FloatMath()){{ 
                    new[] { 1.0f, 2.0f, 3.0f, 4.0f }, new Detection { CameraSerial = "Q2UV-5LPF-HURS", Timestamp = 1706679450040, Output = new Output { Class = 1, Id = 1, Location = new[] { 1.0f, 2.0f, 3.0f, 4.0f }, Score = 8.75f } }
         }};
         var tree2 = new KdTree<float, Detection>(4, new FloatMath()){{
                    new[] { 4.0f, 5.0f, 6.0f, 7.0f }, new Detection { CameraSerial = "Q2UV-5LPF-HURS", Timestamp = 1708679460040, Output = new Output { Class = 1, Id = 1, Location = new[] { 4.0f, 5.0f, 6.0f, 7.0f }, Score = 8.75f } }
         }};
         var tree3 = new KdTree<float, Detection>(4, new FloatMath()){{
                    new[] { 1.0f, 2.0f, 3.0f, 4.0f }, new Detection { CameraSerial = "Q2UV-9LPF-KURS", Timestamp = 1708679470040, Output = new Output { Class = 1, Id = 1, Location = new[] { 1.0f, 2.0f, 3.0f, 4.0f }, Score = 8.75f } }
         }};

         violationTreeMock.Object.Add("1,2,3,4,Q2UV-5LPF-HURS", tree1);
         violationTreeMock.Object.Add("4,5,6,7,Q2UV-5LPF-HURS", tree2);
         violationTreeMock.Object.Add("1,2,3,4,Q2UV-9LPF-KURS", tree3);

         inferenceRules = new InferenceRules(modelConfig, null, mockLogger.Object, mockConfiguration.Object, violationTreeMock.Object);
     }

     [Fact]
     public void CheckViolation_WhenNoTimeoutViolation_ReturnsTrue()
     {
        //Arrange
        var output = new Output { Location = new float[] { 1.0f, 2.0f, 3.0f, 4.0f } };
        var cameraSerial = "Q2UV-5LPF-HURS";
        var timeStamp = 1706679640040;

        //Act
        var result = inferenceRules.checkViolation(output, cameraSerial, timeStamp);

        //Assert
        Assert.True(result);
     }

     [Fact]
     public void CheckViolation_WhenTimeoutViolationDetected_ReturnFalse()
     {
        //Arrange
        var output = new Output { Location = new float[] { 1.0f, 2.0f, 3.0f, 4.0f } };
        var cameraSerial = "Q2UV-5LPF-HURS";
        var timeStamp = 1706679600040;

        //Act
        var result = inferenceRules.checkViolation(output, cameraSerial, timeStamp);

        //Assert
        Assert.False(result);
     }

     [Fact]
     public void CheckViolation_Neighbors_DifferentCamera_ReturnTrue()
     {
        //Arrange
        var output = new Output { Location = new float[] { 4.0f, 5.0f, 6.0f, 7.0f } };
        var cameraSerial = "Q2UV-9LPF-KURS";
        var timeStamp = 1706679600040;

        //Act
        var result = inferenceRules.checkViolation(output, cameraSerial, timeStamp);

        //Assert
        Assert.True(result);
     }

    [Fact]
     public void FilterNeighbors_Returns_Filtered_Neighbors()
     {
        // Arrange
        var cameraSerial = "Q2UV-5LPF-HURS";
        var neighbors = new[]
        {
            new KdTreeNode<float, Detection>(new float[] {0.1f, 0.2f, 0.3f, 0.4f}, new Detection { CameraSerial = "Q2UV-5LPF-HURS" }),
            new KdTreeNode<float, Detection>(new float[] {0.5f, 0.6f, 0.7f, 0.8f}, new Detection { CameraSerial = "Q2UV-9LPF-HURS" }),
            new KdTreeNode<float, Detection>(new float[] {0.9f, 0.8f, 0.7f, 0.6f}, new Detection { CameraSerial = "Q2UV-5LPF-HURS" }),
        };

        // Act
        var filtered = inferenceRules.filterNeighbors(neighbors, cameraSerial);

        // Assert
        Assert.Equal(2, filtered.Length);
        Assert.All(filtered, n => Assert.Equal(cameraSerial, n.Value.CameraSerial));
     }

}
