using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace VideoAnalyticsPipeline;

public class MQTTBroker(
    ILogger<MQTTBroker> logger,
    IHostApplicationLifetime hostApplicationLifetime,
    IConfiguration configuration,
    ChannelFactory channelFactory) : IModule
{
    private readonly IMqttClient _mqttClient = new MqttFactory().CreateMqttClient();
    async Task InitAsync(CancellationToken cancellationToken)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(configuration["MQTT:Host"])//, Convert.ToInt16(configuration["MQTT:Port"]))
            .Build();

        _mqttClient.ConnectedAsync += (e) =>
        {
            logger.LogInformation("Connected to MQTT Broker @{Host}", configuration["MQTT:Host"]);
            return Task.CompletedTask;
        };

        _mqttClient.ApplicationMessageReceivedAsync += (e) => Process(e, cancellationToken);

        _mqttClient.DisconnectedAsync += (e) =>
        {
            logger.LogInformation("Disconnected from MQTT Broker!");
            hostApplicationLifetime.StopApplication();
            return Task.CompletedTask;
        };

        await _mqttClient.ConnectAsync(options, cancellationToken);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InitAsync(cancellationToken);

            var topics = configuration.GetSection("MQTT:Topics").Get<string[]>();

            if (topics == null || topics.Length == 0) throw new Exception("No topics configured");

            foreach (var topic in topics)
            {
                await _mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic(topic).Build(),
                    cancellationToken);

                logger.LogInformation("Subscribed to topic {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing MQTT Broker");
        }
    }
    async Task Process(MqttApplicationMessageReceivedEventArgs e, CancellationToken cancellationToken)
    {
        try
        {
            if (e.ApplicationMessage.PayloadSegment.Array == null) throw new Exception("Payload is null");

            logger.LogInformation("Received message from topic {Topic}. Message {Message}",
                   e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());

            // invoke pipeline 
            foreach (var channel in channelFactory.Writers(nameof(MQTTBroker)))
            {
                var camSerial = e.ApplicationMessage.Topic.Split('/')[2];
                var inference = await ObjectFormatter.DeserializeAsync<Inference>(e.ApplicationMessage.PayloadSegment.Array, cancellationToken);
                await channel.WriteAsync(new Data { Inference = inference, CameraSerial = camSerial }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message");
        }
    }
}
