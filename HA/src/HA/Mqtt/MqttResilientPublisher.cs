using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;

namespace HA.Mqtt;

public class MqttResilientPublisher : IMqttResilientPublisher
{
    private readonly ILogger _logger;
    private readonly IMqttClient _client;
    private readonly MqttFactory _mqttFactory = new();
    private readonly MqttClientOptions _clientOptions;
    private readonly ConcurrentQueue<MqttApplicationMessage> _messages = new();
    private readonly object _lock = new();
    private Task? _task;
    private bool _disposed = false;

    public string? ClientId { get; private set; }

    public bool IsConnected => _client != null ? _client.IsConnected : false;

    public MqttResilientPublisher(ILogger logger, MqttClientOptions clientOptions, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _logger = logger;
        _clientOptions = clientOptions;
        _client = _mqttFactory.CreateMqttClient();
    }

    public MqttResilientPublisher(ILogger logger, string host, int port = 1883, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _logger = logger;
        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(ClientId)
            .Build();
        _client = _mqttFactory.CreateMqttClient();
    }

    public void Disconnect()
    {
        AsyncHelper.RunSync(() => _client.DisconnectAsync());
    }

    public void Publish(string topic, string payload)
    {
        EnsureTaskIsRunning();
        _messages.Enqueue(CreateMessage(topic, payload));
    }

    public void Publish(IEnumerable<Tuple<string, string>> topicsAndpPayloads)
    {
        EnsureTaskIsRunning();
        foreach (var topicAndPayload in topicsAndpPayloads)
        {
            _messages.Enqueue(CreateMessage(topicAndPayload.Item1, topicAndPayload.Item2));
        }
    }

    public void Publish(Measurement measurement)
    {
        EnsureTaskIsRunning();
        var baseTopic = $"measurement/{measurement.Device}/";

        var messages = new List<MqttApplicationMessage>();
        _messages.Enqueue(CreateMessage($"{baseTopic}/quality", measurement.Quality.ToString()));
        _messages.Enqueue(CreateMessage($"{baseTopic}/time", measurement.GetUtcTimeStamp().ToLongTimeString()));
        foreach (var tag in measurement.Tags)
            _messages.Enqueue(CreateMessage($"{baseTopic}/{tag.Key}", tag.Value));
        foreach (var value in measurement.Values)
            if (value.Value != null)
                _messages.Enqueue(CreateMessage($"{baseTopic}/{value.Name}", $"{value.Value}"));
    }

    private void EnsureTaskIsRunning()
    {
        if (_task == null)
        {
            lock (_lock)
            {
                if (_task == null)
                {
                    CreateTask();
                }
            }
        }
    }

    private void CreateTask()
    {
        _logger.LogDebug("Start a background MQTT publisher task");
        _task = Task.Factory.StartNew(
            DoWorkAsync,
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskScheduler.Default);
    }

    private async Task DoWorkAsync()
    {
        int count = 0;
        while (!_disposed)
        {
            var isConnected = await EnsureConnectedAsync();
            if (isConnected)
            {
                while (_messages.Count > 0)
                {
                    if (_messages.TryPeek(out var message))
                    {
                        if (await PublishMessageAsync(message))
                        {
                            _messages.TryDequeue(out message);
                            _logger.LogDebug("Published MQTT message {0}", ++count);
                        }
                    }
                }
                Thread.Sleep(100);
            }
            else
            {
                Thread.Sleep(5000);
            }
        }
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
            _logger.LogDebug("published MQTT message: {0} | {1}", result.ReasonCode, message.Topic);
        }
        return result.IsSuccess;
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        try
        {
            if (!_client.IsConnected)
            {
                var connectResult = await _client.ConnectAsync(_clientOptions, CancellationToken.None);
                _logger.LogDebug("try to connect to MQTT broker: {0} | {1}", connectResult.ReasonString, connectResult.ResultCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("MQTT Client Exception: {0}", ex.Message);
        }
        return _client.IsConnected;
    }
}