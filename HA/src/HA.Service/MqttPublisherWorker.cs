using HA.Mqtt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HA.Service;

public class MqttPublisherWorker : BackgroundService, IObserverProcessor
{
    private readonly ILogger _logger;
    private readonly MqttPublisher _mqttPublisher;
    private readonly ConcurrentQueue<Measurement> _measurementQueue = new ();
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    /// <summary>
    /// Decide if we want to publish a Measurement as a JSON payload
    /// Default = false
    /// </summary>
    /// <remarks>would be published to the topic json/measurements/{device}</remarks>
    public bool PublishJson { get; set; } = false;

    /// <summary>
    /// Decide if we want to publish a Measurement as a LineProtocol payload
    /// Default = false
    /// </summary>
    /// <remarks>would be published to the topic lineprotocol/measurments/{device}</remarks>
    public bool PublishLineProtocol { get; set; } = false;

    /// <summary>
    /// Decide if we want to publish a Measurement as a list
    /// Default = true
    /// </summary>
    /// <example>measurements/device</example>
    /// <remarks>topic would be:
    ///   measurements/{device}/value1
    ///   measurements/{device}.value2
    ///   measurements/{device}.value3
    /// </remarks>
    public bool PublishValueList { get; set; } = true;

    public ValueWithStatistic<int> CountJsonPublished { get; set; } = new ValueWithStatistic<int>(0);
    public ValueWithStatistic<int> CountLineProtocolPublished { get; set; } = new ValueWithStatistic<int>(0);
    public ValueWithStatistic<int> CountValueListPublished { get; set; } = new ValueWithStatistic<int>(0);
    public ValueWithStatistic<int> CountError { get; set; } = new ValueWithStatistic<int>(0);
    public ValueWithStatistic<bool> IsConnected { get; set; } = new ValueWithStatistic<bool>(false);

    public MqttPublisherWorker(ILogger<MqttPublisherWorker> logger, MqttPublisher mqttPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mqttPublisher = mqttPublisher ?? throw new ArgumentNullException(nameof(mqttPublisher));
    }

    public void ProcessMeasurement(Measurement measurement)
    {
        _measurementQueue.Enqueue(measurement);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastCheckTime = DateTime.Now;
        var checkInterval = TimeSpan.FromMinutes(1);
        var lastReportTime = DateTime.Now;
        var reportInterval = TimeSpan.FromMinutes(1);
        var isConnected = await IsConnectedToMqttBroker();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now > lastCheckTime + checkInterval)
            {
                isConnected = await IsConnectedToMqttBroker();
                lastCheckTime = DateTime.Now;
            }
            if (DateTime.Now > lastReportTime + reportInterval)
            {
                _logger.LogInformation("{0} Measurment Queue Count: {1}",ThreadIdString, _measurementQueue.Count);
                lastReportTime = DateTime.Now;
            }
            while (_measurementQueue.Count > 0)
            {
                if (isConnected && _measurementQueue.TryPeek(out var measurement))
                {
                    try
                    {
                        _logger.LogInformation("{0} Process measurement: {1}", ThreadIdString, measurement.ToString());
                        if (PublishJson)
                        {
                            var topic = $"json/measurments/{measurement.Device}";
                            await _mqttPublisher.PublishAsync(topic, measurement.ToJson());
                            CountJsonPublished.Value++;
                            _logger.LogInformation("{0} MQTT Publish to Topic: {1} | ChangeCount: {2} in {3}s", 
                                ThreadIdString, topic, CountJsonPublished.DurationCount, (int)CountJsonPublished.CountTimeSpan.TotalSeconds);
                        }
                        if (PublishLineProtocol)
                        {
                            var topic = $"lineprotocol/measurments/{measurement.Device}";
                            await _mqttPublisher.PublishAsync(topic, measurement.ToLineProtocol());
                            CountLineProtocolPublished.Value++;
                            _logger.LogInformation("{0} MQTT Publish to Topic: {1} | ChangeCount: {2} in {3}s",
                                ThreadIdString, topic, CountLineProtocolPublished.DurationCount, (int)CountLineProtocolPublished.CountTimeSpan.TotalSeconds);
                        }
                        if (PublishValueList)
                        {
                            await _mqttPublisher.PublishAsync(measurement);
                            CountValueListPublished.Value++;
                            _logger.LogInformation("{0} MQTT Publish to Topic: measurement/{1}/value | ChangeCount: {2} in {3}s", 
                                ThreadIdString, measurement.Device, CountValueListPublished.DurationCount, (int)CountValueListPublished.CountTimeSpan.TotalSeconds);
                        }
                        _measurementQueue.TryDequeue(out measurement);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical("{0} Error Nats PublishAsync: {1}", ThreadIdString, ex.Message);
                        _logger.LogInformation("{0} Wait 30 seconds", ThreadIdString);
                        CountError.Value++;
                        await Task.Delay(30000);
                    }
                }
                else
                    break;
            }
            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task<bool> IsConnectedToMqttBroker()
    {
        var connected = false;
        try
        {
            connected = await _mqttPublisher.IsConnectedAsync();
         }
        catch (Exception ex)
        {
            _logger.LogCritical("{0} Error: {1}", ThreadIdString, ex.Message);
        }
        _logger.LogDebug("{0} Connected to Nats Server: {1}", ThreadIdString, connected);
        IsConnected.Value = connected;
        return connected;
    }
}
