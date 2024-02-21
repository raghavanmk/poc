using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VideoAnalyticsPipeline;
internal class SQLLogger(
    IConfiguration configuration,
    ChannelFactory channelFactory,
    ILogger<SQLLogger> logger,
    SqlConnection sqlConnection) : IModule
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var data in channelFactory.Reader(nameof(SQLLogger)).ReadAllAsync(cancellationToken))
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

                        // bbleft = xmin, bbright = xmax, bbtop = ymin, bbbottom = ymax
                        // location array sequence is [ymin, xmin, ymax, xmax]                        

                        string insertQuery =
                       $"""
                            INSERT INTO {configuration["Log:Table"]} (CameraSerial,Class,DetectionId,DetectionThreshold,BoundingBoxRight,
                            BoundingBoxLeft,BoundingBoxTop,BoundingBoxBottom,DetectionUnixEpoch,DetectionDateTime, DetectionImageUrl, ModifiedBy, ModifiedDate)
                            VALUES ('{data.CameraSerial}',{o.Class},{o.Id},{o.Score},{o.Location![1]},{o.Location[3]},{o.Location[0]},{o.Location[2]},{unixEpoch},'{dateTime}',
                            null,'cedevops',GETDATE())
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
}
