using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;

namespace HA.Mqtt;

public class MqttConsumer : IMqttConsumer
{
    private readonly ILogger<MqttConsumer> _logger;
    private readonly MqttFactory _mqttFactory = new();
    private readonly MqttClientOptions _clientOptions;
    private readonly ConcurrentQueue<Tuple<string, string>> _messages = new();

    public string? ClientId { get; private set; }

    public MqttConsumer(ILogger<MqttConsumer> logger, MqttClientOptions clientOptions, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _logger = logger;
        _clientOptions = clientOptions;
    }

    public MqttConsumer(ILogger<MqttConsumer> logger, string host, int port = 1883, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _logger = logger;
        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(ClientId)
            .Build();
    }

    public async Task<bool> SubscribeAsync(string topic)
    {
        using (var mqttClient = _mqttFactory.CreateMqttClient())
        {
            await mqttClient.ConnectAsync(_clientOptions, CancellationToken.None);
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                _logger.LogDebug(e.ApplicationMessage.ToString());
                return Task.CompletedTask;
            };
            var mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic(topic); })
                .Build();
            var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        }
        return true;
    }
}