﻿using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace VideoAnalyticsPipeline;
internal class BlobStorage(ChannelFactory channelFactory, IConfiguration configuration,ILogger<BlobStorage> logger) : IModule
{
    private readonly BlobServiceClient blobServiceClient = new BlobServiceClient(configuration["Blob:ConnectionString"]);
    private readonly string containerName = configuration["Blob:ContainerName"]!;
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(BlobStorage).FullName!;
        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                var image = (Image)data;

                if (image != null)
                    await UploadImageToBlobStorage(image, cancellationToken);

                else
                    logger.LogError("Inferred image not available to store in blob for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error storing image for {camSerial} at {timestamp}", data.CameraSerial, data.Inference!.Timestamp);
            }
        }
    }

    private async Task UploadImageToBlobStorage(Image image, CancellationToken cancellationToken)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient($"{image.CameraSerial}_{image.Inference!.Timestamp}_{Guid.NewGuid()}.jpg");

        await blobClient.UploadAsync(image.ImageStream, true, cancellationToken);

        logger.LogInformation("Image uploaded to Azure Blob Storage.");
    }

}
