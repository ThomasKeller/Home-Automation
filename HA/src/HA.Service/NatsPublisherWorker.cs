using HA.Nats;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HA.Service;

public class NatsPublisherWorker : BackgroundService, IObserverProcessor
{
    private readonly ILogger _logger;
    private readonly NatsPublisher _natsPublisher;
    private readonly bool _lineProtocol;
    private readonly ConcurrentQueue<Measurement> _measurementQueue = new ();
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]"; 

    public NatsPublisherWorker(ILogger<NatsPublisherWorker> logger, NatsPublisher natsPublisher, bool lineProtocol = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _natsPublisher = natsPublisher ?? throw new ArgumentNullException(nameof(natsPublisher));
        _lineProtocol = lineProtocol;
    }

    public ValueWithStatistic<int> CountPublished { get; set; } = new ValueWithStatistic<int>(0);
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
        var isConnected = await IsConnectedToNatsServer();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now > lastCheckTime + checkInterval)
            {
                isConnected = await IsConnectedToNatsServer();
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
                        var subject = $"measurements.info.{measurement.Device}";
                         await _natsPublisher.PublishAsync(subject, measurement, _lineProtocol);
                        CountPublished.Value++;
                        _logger.LogInformation("{0} Nats Publish to Subject: {1} | ChangeCount: {2} in {3}s",
                            ThreadIdString, subject, CountPublished.DurationCount, (int)CountPublished.CountTimeSpan.TotalSeconds);
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

    private async Task<bool> IsConnectedToNatsServer()
    {
        var connected = false;
        try
        {
            connected = await _natsPublisher.IsConnectedAsync();
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
