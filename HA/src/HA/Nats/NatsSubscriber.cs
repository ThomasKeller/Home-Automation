using HA.Observable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace HA.Nats;

public class NatsSubscriber : ObservableBase<Measurement>
{
    public class Parameters
    {
        public Parameters(NatsOpts natsOpts, string filteredSubject)
        {
            NatsOptions = natsOpts;
            FilteredSubject = filteredSubject;
        }

        public NatsOpts NatsOptions { get; private set; }
        public string? QueueGroup { get; set; }
        public string? ConsumerName { get; set; }
        public string? StreamName { get; set; }
        public string? FilteredSubject { get; private set; }
    } 

    private readonly ILogger _logger;
    private readonly Parameters _parameters;
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public NatsSubscriber(ILogger logger, Parameters parameters)
    {
        _logger = logger;
        _parameters = parameters;
    }

    public ValueWithStatistic<int> MeasurementsReceived { get; set; } = new ValueWithStatistic<int>(0);

    public ValueWithStatistic<int> ErrorCount { get; set; } = new ValueWithStatistic<int>(0);

    public async Task SubscribeAsync(string subject, string? queueGroup = null, CancellationToken stoppingToken = default)
    {
        var natsUtils = new NatsUtils(_logger);
        var connection = await natsUtils.CreateConnectionAsync(_parameters.NatsOptions, 5, 5);
        if (_parameters.ConsumerName != null && 
            _parameters.StreamName != null && 
            _parameters.FilteredSubject != null)
        {
            var consumer = await natsUtils.CreateOrUpdateConsumerAsync(
                    connection,
                    consumerName: _parameters.ConsumerName,
                    streamName: _parameters.StreamName,
                    filteredSubject: _parameters.FilteredSubject);
            _logger.LogInformation("{0} Stream Cluster: {1} Consumer info: {2}",
                    ThreadIdString, consumer.Info.Cluster?.ToString(), consumer.Info.Config.ToString());
            _logger.LogInformation("{0} Stream Name: {1} Subject: {2}, Queue Group: {3}",
                    ThreadIdString, consumer.Info.StreamName, subject, queueGroup);
            _logger.LogInformation("{0} No. Messages: Redelivered: {1} Pending: {2} Waiting: {3} AckPending: {4}",
                    ThreadIdString, consumer.Info.NumRedelivered, consumer.Info.NumPending, consumer.Info.NumWaiting, consumer.Info.NumAckPending);

            var reportInterval = TimeSpan.FromSeconds(60);
            var lastReport = DateTime.Now;
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeMessageAsync(consumer, stoppingToken);
                if (DateTime.Now > lastReport + reportInterval)
                {
                    lastReport = DateTime.Now;
                    _logger.LogInformation("{0} No. Messages: Redelivered: {1} Pending: {2} Waiting: {3} AckPending: {4}",
                        ThreadIdString, consumer.Info.NumRedelivered, consumer.Info.NumPending,
                        consumer.Info.NumWaiting, consumer.Info.NumAckPending);
                    _logger.LogInformation("{0} Measurements received: {1}",
                        ThreadIdString, MeasurementsReceived.ToShortString());
                }
            }
        }
        else
        {
            _logger.LogInformation("{0} Cluster: {1} Stream Available: {2} | Subcribe without stream support.",
                ThreadIdString, connection.ServerInfo?.Cluster, connection.ServerInfo?.JetStreamAvailable);
            while (!stoppingToken.IsCancellationRequested)
            {
                //lastLog = DateTime.Now;
                await foreach (var msg in connection.SubscribeAsync<string>(subject, queueGroup))
                    ProccessMessage(msg);
            }
        }
    }

    private async Task ConsumeMessageAsync(INatsJSConsumer consumer, CancellationToken stoppingToken)
    {
        try
        {
            var next = await consumer.NextAsync<string>(cancellationToken: stoppingToken);
            var value = next.GetValueOrDefault();
            if (!string.IsNullOrEmpty(value.Data))
            {
                try
                {
                    _logger.LogDebug("received header:  {0}", value.Headers);
                    _logger.LogDebug("received payload: {0}", value.Data);
                    var payloadIsJson = true; // default
                    if (value.Headers?.ContainsKey("PayloadType") ?? false)
                    {
                        payloadIsJson = value.Headers["PayloadType"].FirstOrDefault() == "JSON";
                    }  
                    var measurement = payloadIsJson
                        ? Measurement.FromJson(value.Data)
                        : Measurement.FromLineProtocol(value.Data);
                    if (measurement != null)
                    {
                        _logger.LogInformation(measurement.ToString());
                        MeasurementsReceived.Value++;
                        ExecuteOnNext(measurement);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    ErrorCount.Value++;
                    ExecuteOnError(ex);
                }
                await value.AckAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            ErrorCount.Value++;
            ExecuteOnError(ex);
        }
    }

    private void ProccessMessage(NatsMsg<string> msg)
    {
        if (msg.Data != null)
        {
            try
            {
                var measurement = msg.Data.StartsWith("{") 
                    ? Measurement.FromJson(msg.Data)
                    : Measurement.FromLineProtocol(msg.Data);
                if (measurement != null)
                {
                    _logger.LogInformation("{0} {1}", ThreadIdString, measurement.ToString());
                    MeasurementsReceived.Value++;
                    ExecuteOnNext(measurement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorCount.Value++;
                ExecuteOnError(ex);
            }
        }
        else
            _logger.LogWarning("{0} Empty message received.", ThreadIdString);
    }
}
