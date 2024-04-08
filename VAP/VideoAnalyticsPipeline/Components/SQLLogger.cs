using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs.Models;

namespace VideoAnalyticsPipeline;
internal class SQLLogger(
    IConfiguration configuration,
    ChannelFactory channelFactory,
    ILogger<SQLLogger> logger,
    SqlConnection sqlConnection) : IModule
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(SQLLogger).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            try
            {
                await LogSql(data, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error logging message {Inference} to SQL Server for camera {CameraSerial} at {Timestamp}",
                    data.Inference!.ToString(), data.CameraSerial, data.Inference!.Timestamp);
            }
        }
    }

    internal async ValueTask LogSql(Data data, CancellationToken cancellationToken)
    {
        if (data.ViolationDetected || configuration["Log:All"] == "true")
        {
            if (data.Inference!.Outputs != null)
            {
                logger.LogInformation("Writing to SQL Server");

                try
                {
                    await sqlConnection.OpenAsync(cancellationToken);

                    foreach (var o in data.Inference.Outputs)
                    {
                        var unixEpoch = data.Inference.Timestamp;
                        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixEpoch);
                        var imageLink = GenerateDetectionImageUrl(data.CameraSerial!, unixEpoch);

                        // bbleft = xmin, bbright = xmax, bbtop = ymin, bbbottom = ymax
                        // location array sequence is [xmin, ymin, xmax, ymax]                        

                        string insertQuery =
                       $"""
                            INSERT INTO {configuration["Log:Table"]} (CameraSerial,Class,DetectionId,DetectionThreshold,BoundingBoxRight,
                            BoundingBoxLeft,BoundingBoxTop,BoundingBoxBottom,DetectionUnixEpoch,DetectionDateTime, DetectionImageUrl, ModifiedBy, ModifiedDate)
                            VALUES ('{data.CameraSerial}',{o.Class},{o.Id},{o.Score},{o.Location![2]},{o.Location[0]},{o.Location[1]},{o.Location[3]},{unixEpoch},'{dateTime}',
                            {imageLink},'cedevops',GETDATE())
                        """;

                        using var command = new SqlCommand(insertQuery, sqlConnection);

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                finally
                {
                    await sqlConnection.CloseAsync();
                }
            }
        }
    }

    private string GenerateDetectionImageUrl(string cameraSerial, long unixEpoch)
    {
        var blobServiceClient = new BlobServiceClient(configuration["Blob:ConnectionString"]);
        var containerClient = blobServiceClient.GetBlobContainerClient(configuration["Blob:ContainerName"]);

        string blobItemName = $"{cameraSerial}_{unixEpoch}.jpg";

        var blobClient = containerClient.GetBlobClient(blobItemName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddYears(100),
            Protocol = SasProtocol.Https
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(configuration["Blob:AccountName"], configuration["Blob:AccountKey"])).ToString();

        var sasUrl = $"{blobClient.Uri}?{sasToken}";

        return sasUrl; 
    }
}
