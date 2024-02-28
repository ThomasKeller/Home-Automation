using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using Polly.Retry;
using Polly;
using System.Collections.Concurrent;
using NATS.Client.JetStream;

namespace HA.Nats;

public class NatsResilientPublisher
{
    private static readonly TaskFactory _taskFactory = new(
        CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
    private readonly ILogger _logger;
    private readonly NatsUtils _natsUtils;
    private readonly NatsOpts _natsOpts;
    private readonly ConcurrentQueue<(string, Measurement, bool)> _measurements = new();
    private readonly object _lock = new();
    private readonly bool _useStream;
    private NatsConnection? _natsConnection;
    private Task? _backgroundTask = null;
    private int _executedTaskCount = 0;
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public NatsResilientPublisher(ILogger logger, NatsOpts natsOpts, bool useStream = true)
    {
        _logger = logger;
        _natsUtils = new NatsUtils(logger);
        _natsOpts = natsOpts;
        _useStream = useStream;
        StartBackgroundTask();
    }

    public NatsResilientPublisher(ILogger logger, NatsConnection natsConnection, bool useStream = true)
    {
        _logger = logger;
        _natsUtils = new NatsUtils(logger);
        _natsConnection = natsConnection;
        _natsOpts = _natsConnection.Opts;
        _useStream = useStream;
        StartBackgroundTask();
    }

    public int ToPublishCount { get; set; }

    public void Publish(string subject, Measurement measurement, bool lineProtocol = false)
    {
        _measurements.Enqueue((subject, measurement, lineProtocol));
    }

    private void StartBackgroundTask()
    {
        if (_backgroundTask == null)
        {
            _backgroundTask = _taskFactory.StartNew(
                DoWorkAsync,
                TaskCreationOptions.None);
        }
}

    private async Task<bool> CheckConnectionAsync()
    {
        if (_natsConnection == null)
        {
            _natsConnection = await _natsUtils.CreateConnectionAsync(_natsOpts, 3, 5, 60);
            return _natsConnection.ConnectionState == NatsConnectionState.Open;
        }
        else
            return await _natsUtils.IsConnectedAsync(_natsConnection);
    }

    private async Task<bool> RetryCheckConnectionAsync(int maxRetryAttempts = 5, int delay_s = 10, int timeout_s = 120)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions() {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(delay_s) })
            .AddTimeout(TimeSpan.FromSeconds(timeout_s))
            .Build();
        return await pipeline.ExecuteAsync(async token => {
            return await CheckConnectionAsync();
        }, CancellationToken.None);
    }

    private async Task DoWorkAsync()
    {
        _logger.LogInformation("{0} Start worker task number {1}", ThreadIdString, ++_executedTaskCount);
        INatsJSContext? context = null;
        var connected = _natsConnection != null && _natsConnection.ConnectionState == NatsConnectionState.Open;
        while (true)
        {
            while (!connected)
            {
                _logger.LogInformation("{0} Connecting to nats server/cluster: {1}", ThreadIdString, connected);
                connected = await RetryCheckConnectionAsync(maxRetryAttempts: 2, delay_s: 10, timeout_s: 30);
                _logger.LogInformation("{0} Connected to nats server/cluster: {1}", ThreadIdString, connected);
            }
            if (_useStream && context == null && _natsConnection != null)
            {
                context = await _natsUtils.GetContextAsync(_natsConnection);
            }
            while (_measurements.Count > 0 && _natsConnection != null)
            {
                if (_measurements.TryPeek(out var measurementInfos))
                {
                    try
                    {
                        if (_useStream && context != null)
                        {
                            await _natsUtils.PublishAsync(
                                context,
                                subject: measurementInfos.Item1,
                                measurement: measurementInfos.Item2,
                                lineProtocol: measurementInfos.Item3);
                        }
                        else
                        {
                            await _natsUtils.PublishAsync(
                                _natsConnection,
                                subject: measurementInfos.Item1,
                                measurement: measurementInfos.Item2,
                                lineProtocol: measurementInfos.Item3);
                        }
                        _logger.LogInformation("{0} Measurement published: {1}", ThreadIdString, measurementInfos.Item2.ToString());
                        _measurements.TryDequeue(out measurementInfos);
                    }
                    catch (Exception)
                    {
                        connected = false;
                        break;
                    }
                }
            }
            var waitCount = 300;
            while(_measurements.Count == 0)
            {
                Thread.Sleep(100);
                if (waitCount-- <= 0)
                    break;
            }
        }
    }
}