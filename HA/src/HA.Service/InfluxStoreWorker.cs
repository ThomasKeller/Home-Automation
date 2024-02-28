using HA.Influx;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HA.Service;

public class InfluxStoreWorker : BackgroundService, IObserverProcessor
{
    private readonly ILogger _logger;
    private readonly IInfluxStore _influxStore;
    private readonly ConcurrentQueue<Measurement> _measurementQueue = new ();
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public InfluxStoreWorker(ILogger<InfluxStoreWorker> logger, IInfluxStore influxStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _influxStore = influxStore ?? throw new ArgumentNullException(nameof(influxStore));
    }

    public ValueWithStatistic<int> CountStoredMeasurements { get; set; } = new ValueWithStatistic<int>(0);
    public ValueWithStatistic<int> CountError { get; set; } = new ValueWithStatistic<int>(0);
    public ValueWithStatistic<bool> IsConnected { get; set; } = new ValueWithStatistic<bool>(false);

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
        var isConnected = CheckHealth();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now > lastCheckTime + checkInterval)
            {
                isConnected = CheckHealth();
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
                        _logger.LogInformation("{0} MQTT Publish to Topic: measurement/{1}/value", ThreadIdString, measurement.Device);
                        _influxStore.WriteMeasurement(measurement);
                        CountStoredMeasurements.Value++;
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

    private bool CheckHealth()
    {
        var connected = false;
        try
        {
            connected = _influxStore.CheckHealth();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("{0} Error: {1}", ThreadIdString, ex.Message);
        }
        _logger.LogDebug("{0} Connected to Influs: {1}", ThreadIdString, connected);
        IsConnected.Value = connected;
        return connected;
    }
}
