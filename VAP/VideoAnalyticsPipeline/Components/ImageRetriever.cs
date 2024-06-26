﻿using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace VideoAnalyticsPipeline;

internal class ImageRetriever(ChannelFactory channelFactory, ILogger<ImageRetriever> logger, MerakiAPIProxy proxy) : IModule
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(ImageRetriever).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                if (!data.ViolationDetected) continue;

                var image = await DownloadSnapshot(data, cancellationToken);

                if (image == null) continue;

                foreach (var channel in channelFactory.Writers(currentComponent))
                {
                    await channel.WriteAsync(image, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving image for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
        }
    }

    internal async ValueTask<Image?> DownloadSnapshot(Data data, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, string>
        {
            { "timestamp", DateTimeOffset.FromUnixTimeMilliseconds(data.Inference!.Timestamp).UtcDateTime.ToString("o") }, // 2023-12-13T05:26:22.0000000Z
            { "fullframe", "false"}
        };

        logger.LogInformation("Retrieving Image Url {timestamp}", data.Inference!.Timestamp);

        var result = await proxy.GetImageUrl(payload, data.CameraSerial!, cancellationToken);

        var jsonDocument = JsonDocument.Parse(result);

        if (!jsonDocument.RootElement.TryGetProperty("url", out var urlElement)) return null;

        var urlValue = urlElement.GetString();

        if (urlValue == null) return null;

        logger.LogInformation("Retrieving Image {timestamp}", data.Inference!.Timestamp);

        var imageStream = await proxy.GetImage(urlValue, cancellationToken);

        var image = new Image
        {
            CameraSerial = data.CameraSerial,
            ImageStream = imageStream,
            Inference = data.Inference,
            ViolationDetected = data.ViolationDetected,
            ConfinedSpace = data.ConfinedSpace
        };

        return image;

    }
}
