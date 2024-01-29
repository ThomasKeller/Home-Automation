using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;

namespace HA.Mqtt;

public class MqttPublisher : IMqttPublisher, IObserverProcessor
{
    private readonly ILogger<MqttPublisher> _logger;
    private readonly IMqttClient _client;
    private readonly MqttFactory _mqttFactory = new();
    private readonly MqttClientOptions _clientOptions;
    private readonly ConcurrentQueue<Tuple<string, string>> _messages = new();
    private DateTime _lastConnectTime = DateTime.MinValue;

    public string? ClientId { get; private set; }

    public bool IsConnected => _client != null ? _client.IsConnected : false;

    public MqttPublisher(ILogger<MqttPublisher> logger, MqttClientOptions clientOptions, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _logger = logger;
        _clientOptions = clientOptions;
        _client = _mqttFactory.CreateMqttClient();
        var task = Task.Factory.StartNew(() => EnsureConnectedAsync(),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);
    }

    public MqttPublisher(ILogger<MqttPublisher> logger, string host, int port = 1883, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _logger = logger;
        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(ClientId)
            .Build();
        _client = _mqttFactory.CreateMqttClient();
    }

    public async Task DisconnectAsync()
    {
        await _client.DisconnectAsync();
    }

    public async Task<bool> PublishAsync(string topic, string payload)
    {
        var isConnected = await EnsureConnectedAsync();
        if (isConnected)
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();
            var result = await _client.PublishAsync(applicationMessage, CancellationToken.None);
            _logger.LogInformation("published MQTT message: {0} | {1}", result.ReasonCode, topic);
            return result.IsSuccess;
        }
        return false;
    }

    public async Task<bool> PublishAsync(IDictionary<string, string> topicsAndpPayloads)
    {
        var isConnected = await EnsureConnectedAsync();
        if (isConnected)
        {
            foreach (var topicAndPayload in topicsAndpPayloads)
            {
                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topicAndPayload.Key)
                    .WithPayload(topicAndPayload.Value)
                    .Build();
                var result = await _client.PublishAsync(applicationMessage, CancellationToken.None);
                if (!result.IsSuccess)
                {
                    _logger.LogDebug("published MQTT message: {0} | {1}", result.ReasonCode, topicAndPayload.Key);
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    public async Task<bool> PublishAsync(Measurement measurement)
    {
        var isConnected = await EnsureConnectedAsync();
        if (isConnected)
        {
            var baseTopic = $"measurements/{measurement.Device}";

            var messages = new List<MqttApplicationMessage> {
                CreateMessage($"{baseTopic}/quality", measurement.Quality.ToString()),
                CreateMessage($"{baseTopic}/time", measurement.GetUtcTimeStamp().ToString("s"))
            };
            foreach (var tag in measurement.Tags)
                messages.Add(CreateMessage($"{baseTopic}/{tag.Key}", tag.Value));
            foreach (var value in measurement.Values)
                if (value.Value != null)
                {
                    if (value.Value is DateTime dt)
                        messages.Add(CreateMessage($"{baseTopic}/{value.Name}", dt.ToString("s")));
                    else
                        messages.Add(CreateMessage($"{baseTopic}/{value.Name}", $"{value.Value}"));
                }
            foreach (var message in messages)
            {
                if (!await PublishMessageAsync(message))
                {
                    return false;
                }
            }
            _logger.LogInformation("published MQTT messages: {0} | {1}", messages.Count, baseTopic);
            return true;
        }
        return false;
    }

    public void ProcessMeasurement(Measurement measurement)
    {
        AsyncHelper.RunSync(() => PublishAsync(measurement));
    }

    private MqttApplicationMessage CreateMessage(string topic, string payload)
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();
    }

    private async Task<bool> PublishMessageAsync(MqttApplicationMessage message)
    {
        var result = await _client.PublishAsync(message, CancellationToken.None);
        if (!result.IsSuccess)
        {
            _logger.LogInformation("published MQTT message: {0} | {1}", result.ReasonCode, message.Topic);
        }
        return result.IsSuccess;
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        try
        {
            //_client.ReconnectAsync
            if (!_client.IsConnected)
            {
                if (_lastConnectTime == DateTime.MinValue)
                {
                    var connectResult = await _client.ConnectAsync(_clientOptions, CancellationToken.None);
                    if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                    {
                        _lastConnectTime = DateTime.Now;
                    }
                    _logger.LogInformation("try to connect to MQTT broker: {0} | {1}", connectResult.ReasonString, connectResult.ResultCode);
                }
                else
                {
                    await _client.ReconnectAsync(CancellationToken.None);
                    _logger.LogInformation("try to re-connect to MQTT broker: {0}", _client.IsConnected);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("MQTT Client Exception: {0}", ex.Message);
        }
        return _client.IsConnected;
    }
}