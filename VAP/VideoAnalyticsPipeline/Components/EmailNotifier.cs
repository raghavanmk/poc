using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace VideoAnalyticsPipeline;
internal class EmailNotifier(IConfiguration configuration, ChannelFactory channelFactory, MailManager mailManager, ILogger<EmailNotifier> logger) : IModule
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(EmailNotifier).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                var image = (Image)data;

                var cameraName = configuration[$"Camera:{image.CameraSerial}:Location"];

                var classes = data.Inference!.Outputs!.Select(x => x.Class);
                
                var labels = GetLables(classes, configuration);

                if (image != null)
                    await mailManager.SendMail(image.ImageStream!, data.Inference!.ToString(), 
                        image.CameraSerial!, image.Inference!.Timestamp, cameraName!, labels, cancellationToken);
                else
                    logger.LogError("Inferred image not available to send email for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error sending email for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp); 
            }
        }
    }

    private static string GetLables(IEnumerable<int> classes, IConfiguration configuration)
    {
        var labelsBuilder = new StringBuilder();

        foreach (var cls in classes)
        {
            labelsBuilder.Append(configuration[$"LabelMap:{cls}"]);
            labelsBuilder.Append(", ");
        }

        if (labelsBuilder.Length > 0)
        {
            labelsBuilder.Length -= 2;  // Remove the last comma and space
        }
        return labelsBuilder.ToString();

    }
}
