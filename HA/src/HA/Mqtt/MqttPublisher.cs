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
    private string ThreadIdString => $"TID:{Thread.CurrentThread.ManagedThreadId}";

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
            _logger.LogInformation("{0} published MQTT message: {1} | {2}", ThreadIdString, result.ReasonCode, topic);
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
                    _logger.LogDebug("{0} published MQTT message: {1} | {2}", ThreadIdString, result.ReasonCode, topicAndPayload.Key);
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    public async Task<bool> PublishAsync(string topicPrefix, Measurement measurement)
    {
        var isConnected = await EnsureConnectedAsync();
        if (isConnected)
        {
            var baseTopic = topicPrefix.Trim();
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
            _logger.LogInformation("{0} published MQTT messages: {1} | {2}", ThreadIdString, messages.Count, baseTopic);
            return true;
        }
        return false;
    }


    public async Task<bool> PublishAsync(Measurement measurement)
    {
        var topicPrefix = $"measurements/{measurement.Device}";
        return await PublishAsync(topicPrefix, measurement);
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
            _logger.LogInformation("{0} published MQTT message: {1} | {2}", ThreadIdString, result.ReasonCode, message.Topic);
        }
        return result.IsSuccess;
    }

    public async Task<bool> IsConnectedAsync()
    {
        return await EnsureConnectedAsync();
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        try
        {
            if (!_client.IsConnected)
            {
                if (_lastConnectTime == DateTime.MinValue)
                {
                    var connectResult = await _client.ConnectAsync(_clientOptions, CancellationToken.None);
                    if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                    {
                        _lastConnectTime = DateTime.Now;
                    }
                    _logger.LogInformation("{0} try to connect to MQTT broker: {1} | {2}", ThreadIdString, connectResult.ReasonString, connectResult.ResultCode);
                }
                else
                {
                    await _client.ReconnectAsync(CancellationToken.None);
                    _logger.LogInformation("{0} try to re-connect to MQTT broker: {1}", ThreadIdString, _client.IsConnected);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("{0} MQTT Client Exception: {1}", ThreadIdString, ex.Message);
        }
        return _client.IsConnected;
    }
}