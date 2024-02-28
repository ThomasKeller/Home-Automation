using HA.Observable;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using System.Reflection;

namespace HA.Nats;

public class NatsStreamConsumerWorker : BackgroundService, IObservable<Measurement>
{
    private readonly ILogger _logger;
    private readonly List<IObserver<Measurement>> _observers = new();
    private readonly NatsOpts _natsOptions;

    public ValueWithStatistic<int> OnNextCount { get; set; } = new ValueWithStatistic<int>(0);

    public ValueWithStatistic<int> OnErrorCount { get; set; } = new ValueWithStatistic<int>(0);

    public string Subject { get; set; } = "measurements.>";

    public string ConsumerFilteredSubject { get; set; } = "measurements.new.*";

    public string StreamName { get; set; } = "MEASUREMENTS";

    public string ConsumerName { get; set; } = CreateUniqueClientName();

    public uint MaxAgeInDays { get; set; } = 14;

    public uint AckWaitInSeconds { get; set; } = 10;

    public NatsStreamConsumerWorker(ILogger logger, NatsOpts natsOpts)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _natsOptions = natsOpts ?? throw new ArgumentNullException(nameof(natsOpts));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var natStreamCreator = new NatsStreamCreator(_logger);
            var streamExists = await natStreamCreator.CreateStreamAsync(_natsOptions);
            var stream = natStreamCreator.Stream;
            if (!streamExists)
                throw new Exception($"Stream '{natStreamCreator.StreamName}' not found.");
            var streamInfo = stream?.Info;
            if (streamInfo != null)
            {
                _logger.LogInformation("Stream Cluster: {0}", streamInfo.Cluster.ToString());
                _logger.LogInformation("Stream Name: {0} Subject: {1}",
                    streamInfo.Config.Name, streamInfo.Config.Subjects.FirstOrDefault());
            }
            if (stream == null)
                throw new Exception($"Stream '{natStreamCreator.StreamName}' not found.");
            var consumer = await stream.CreateOrUpdateConsumerAsync(new ConsumerConfig(ConsumerName) {
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                AckWait = TimeSpan.FromSeconds(AckWaitInSeconds),
                FilterSubject = ConsumerFilteredSubject
            });
            var consumerInfo = consumer.Info;
            _logger.LogInformation("Consumer Name: {0} Waiting: {1} Pending: {2}",
                consumerInfo.Name, consumerInfo.NumWaiting, consumerInfo.NumPending);
            var lastLog = DateTime.MinValue;
            while (!stoppingToken.IsCancellationRequested)
            {
                lastLog = DateTime.Now;
                try
                {
                    var next = await consumer.NextAsync<string>();// NatsJsonSerializerRegistry.Default, _natsOptions, stoppingToken);
                    var value = next.GetValueOrDefault();
                    if (!string.IsNullOrEmpty(value.Data))
                    {
                        try
                        {
                            _logger.LogDebug("received header:  {0}", value.Headers);
                            _logger.LogDebug("received payload: {0}", value.Data);
                            var payloadIsJson = value.Headers?.ContainsKey("PayloadType") ?? false
                                                && value.Headers["PayloadType"].FirstOrDefault() == "JSON";
                            var measurement = payloadIsJson
                                ? Measurement.FromJson(value.Data)
                                : Measurement.FromLineProtocol(value.Data); ;
                            if (measurement != null)
                            {
                                _logger.LogInformation(measurement.ToJson());
                                ExecuteOnNext(measurement);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                            ExecuteOnError(ex);
                        }
                    }
                    if (value.Data != null)
                        await value.AckAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    ExecuteOnError(ex);
                }
            }
            _logger.LogInformation("ExecuteOnComplete");
            ExecuteOnComplete();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex.Message, ex);
            ExecuteOnError(ex);
            ExecuteOnComplete();
        }
    }

    public IDisposable Subscribe(IObserver<Measurement> observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
        return new Unsubscriber<Measurement>(_observers, observer);
    }

    private void ExecuteOnNext(Measurement value)
    {
        OnNextCount.Value = OnNextCount.Value + 1;
        foreach (var observer in _observers)
            observer?.OnNext(value);
    }

    private void ExecuteOnComplete()
    {
        foreach (var observer in _observers)
            observer?.OnCompleted();
        _observers.Clear();
    }

    private void ExecuteOnError(Exception error)
    {
        OnErrorCount.Value = OnErrorCount.Value + 1;
        foreach (var observer in _observers)
            observer?.OnError(error);
    }

    private static string CreateUniqueClientName()
    {
        var machineName = Environment.MachineName;
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        return $"{machineName}-{assemblyName}";
    }
}