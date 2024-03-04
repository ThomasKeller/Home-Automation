using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Polly.Retry;
using Polly;
using Microsoft.Extensions.Primitives;

namespace HA.Nats;

public class NatsUtils
{
    private static readonly Dictionary<string, StringValues> _headerLP = new Dictionary<string, StringValues>
                    { { "PayloadType", "LineProtocol" }, { "DataType", typeof(Measurement).FullName } };
    private static readonly Dictionary<string, StringValues> _headerJson = new Dictionary<string, StringValues>
                    { { "PayloadType", "JSON" }, { "DataType", typeof(Measurement).FullName } };

    private readonly ILogger _logger;
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public NatsUtils(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Try to create a connection to a nats server/cluster with a polly resilience pattern.
    /// </summary>
    /// <param name="natsOpts">Nats connection options</param>
    /// <param name="maxRetryAttemts">Number of maximal retries</param>
    /// <param name="delayBetweenRetrySec">Time delay between the retries</param>
    /// <param name="maxTimeoutSec">maximal time in seconds before timeout exception</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>NatsConnection</returns>
    public async Task<NatsConnection> CreateConnectionAsync(NatsOpts natsOpts, int maxRetryAttemts,
                                                            int delayBetweenRetrySec = 10, int maxTimeoutSec = 7200)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions() { 
                MaxRetryAttempts = maxRetryAttemts,
                Delay = TimeSpan.FromSeconds(delayBetweenRetrySec)}) 
            .AddTimeout(TimeSpan.FromSeconds(maxTimeoutSec)) 
            .Build();
        return await pipeline.ExecuteAsync(async token => {
                return await CreateConnectionAsync(natsOpts);
            }, CancellationToken.None);
    }

    /// <summary>
    /// Try to create a connection to a nats server/cluster
    /// </summary>
    /// <param name="natsOpts">Nats connection options</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>NatsConnection</returns>
    public async Task<NatsConnection> CreateConnectionAsync(NatsOpts natsOpts)
    {
        try
        {
            var connection = new NatsConnection(natsOpts);
            await connection.ConnectAsync();
            var timeResponse = await connection.PingAsync();
            var serverInfo = connection.ServerInfo;
            _logger.LogInformation("{0} NATS connection state: '{1}' | Ping/Pong time: {2} ms",
                ThreadIdString, connection.ConnectionState, timeResponse.TotalMilliseconds);
            if (serverInfo != null)
            {
                _logger.LogInformation("{0} Server: Name: '{1}' | Version: {2} | Jetstream Enable: {3}",
                    ThreadIdString, serverInfo.Name, serverInfo.Version, serverInfo.JetStreamAvailable);
                _logger.LogInformation("{0} Server: Host: '{1}' | Port: {2} | Id: {3}",
                    ThreadIdString, serverInfo.Host, serverInfo.Port, serverInfo.Id);
                if (serverInfo.ClientConnectUrls != null)
                    _logger.LogInformation("{0} Connected Clients: {1}",
                        ThreadIdString, string.Join(',', serverInfo.ClientConnectUrls));
            }
            return connection;
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Check and return the connection state (Closed, Open, Connecting, Reconnecting)
    /// </summary>
    /// <param name="connection">nats connections</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>connection state</returns>
    /// <exception cref="NatsException"></exception>
    public async Task<NatsConnectionState> GetConnectionStateAsync(NatsConnection connection)
    {
        try
        {
            if (connection.ConnectionState != NatsConnectionState.Open)
            {
                var timeResponse = await connection.PingAsync();
                _logger.LogInformation("{0} NATS connection state: {1} Ping/Pong time: {2} ms",
                    ThreadIdString, connection.ConnectionState, timeResponse.TotalMilliseconds);
            }
            return connection.ConnectionState;
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Check if we are connected to nats server/cluster
    /// </summary>
    /// <param name="connection">nats connections</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>true is connected or false not connected</returns>
    public async Task<bool> IsConnectedAsync(NatsConnection connection)
    {
        var result = await GetConnectionStateAsync(connection);
        return result == NatsConnectionState.Open;
    }

    /// <summary>
    /// Get a context
    /// </summary>
    /// <param name="connection">nats connections</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>NatsStreamItems</returns>
    public async Task<INatsJSContext> GetContextAsync(NatsConnection connection)
    {
        try
        {
            var context = new NatsJSContext(connection);
            var streamNames = new List<string>();
            await foreach (var name in context.ListStreamNamesAsync()) streamNames.Add(name);
            _logger.LogInformation("{0} Available Stream Names: {1}",
                 ThreadIdString, string.Join(", ", streamNames));
            return context;
        }
        catch (NatsJSException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get the nats stream
    /// </summary>
    /// <param name="connection">nats connections</param>
    /// <param name="streamName">name of the stream</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>NatsStreamItems</returns>
    public async Task<NatsStreamItems> GetStreamAsync(NatsConnection connection, string streamName)
    {
        try
        {
            var streamItems = new NatsStreamItems(connection);
            streamItems.Context = new NatsJSContext(connection);
            streamItems.Stream = await streamItems.Context.GetStreamAsync(streamName);
            _logger.LogInformation("{0} Stream '{1}' exists", ThreadIdString, streamName);
            var streamInfo = streamItems.Stream.Info;
            _logger.LogInformation("{0} Stream Cluster: {1}", ThreadIdString, streamInfo.Cluster?.ToString());
            _logger.LogInformation("{0} Stream Name: {1} Subject: {2}",
                 ThreadIdString, streamInfo.Config.Name, streamInfo.Config.Subjects.FirstOrDefault());
            return streamItems;
        }
        catch (NatsJSException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Create a nats stream
    /// </summary>
    /// <param name="connection">nats connections</param>
    /// <param name="streamName">name of the stream</param>
    /// <param name="subject">subject of the stream e.g. 'measurements.>'</param>
    /// <param name="maxAgeInDays">how long messages will be stored in the stream</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>NatsStreamItems</returns>
    public async Task<NatsStreamItems> CreateStreamAsync(NatsConnection connection, string streamName, 
                                                         string subject, int maxAgeInDays = 14, 
                                                         StreamConfigRetention streamConfigRetention = StreamConfigRetention.Workqueue)
    {
        try
        {
            var streamItems = new NatsStreamItems(connection);
            streamItems.Context = new NatsJSContext(connection);
            var streamExists = await CheckStreamExistAsync(streamItems.Context, streamName);
            _logger.LogInformation($"Stream '{streamName}' exists: {streamExists}");
            var streamConfig = new StreamConfig(streamName, new[] { subject });
            streamItems.Stream = streamExists
                ? await streamItems.Context.GetStreamAsync(streamName)
                : await streamItems.Context.CreateStreamAsync(new StreamConfig(streamName, new[] { subject })
                {
                    Retention = streamConfigRetention,
                    MaxAge = TimeSpan.FromDays(maxAgeInDays)
                });
            var streamInfo = streamItems.Stream.Info;
            _logger.LogInformation("{0} Stream Cluster: {1}", ThreadIdString, streamInfo.Cluster?.ToString());
            _logger.LogInformation("{0} Stream Name: {1} Subject: {2}",
                 ThreadIdString, streamInfo.Config.Name, streamInfo.Config.Subjects.FirstOrDefault());
            return streamItems;
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Check if Stream exists
    /// </summary>
    /// <param name="jsContext">nats context</param>
    /// <param name="streamName">stream name</param>
    /// <exception cref="NatsException"></exception>
    /// <returns>true => exists | false => don't exists</returns>
    public async Task<bool> CheckStreamExistAsync(NatsJSContext jsContext, string streamName)
    {
        try
        {
            await foreach (var name in jsContext.ListStreamNamesAsync())
            {
                if (name.Equals(streamName, StringComparison.InvariantCulture))
                    return true;
            }
            return false;
        }
        catch (NatsException ex)
        {
            _logger.LogCritical(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Delete a stream on nats server/cluster
    /// </summary>
    /// <param name="connection">nats connection</param>
    /// <param name="streamName">stream name</param>
    /// <exception cref="NatsException"></exception>
    /// <returns>true => deleted</returns>
    public async Task<bool> DeleteStreamAsync(NatsConnection connection, string streamName)
    {
        try
        {
            var streamItems = new NatsStreamItems(connection);
            streamItems.Context = new NatsJSContext(connection);
            var streamExists = await streamItems.Context.CheckStreamExistAsync(streamName);
            _logger.LogInformation("{0} Stream '{1}' exists: {2}",
                ThreadIdString, streamName, streamExists);
            return await streamItems.Context.DeleteStreamAsync(streamName);
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Create or update nats consumer
    /// </summary>
    /// <param name="connection">nats connection</param>
    /// <param name="consumerName">consumer name</param>
    /// <param name="streamName">stream name</param>
    /// <param name="filteredSubject">filtered subject</param>
    /// <param name="maxWaiting">The number of pulls that can be outstanding on a pull consumer</param>
    /// <returns>nats consumer</returns>
    public async Task<INatsJSConsumer> CreateOrUpdateConsumerAsync(NatsConnection connection, string consumerName,
                                                                   string streamName, string filteredSubject, int maxWaiting = 1)
    {
        try
        {
            var streamItems = new NatsStreamItems(connection);
            streamItems.Context = new NatsJSContext(connection);
            streamItems.Stream = await streamItems.Context.GetStreamAsync(streamName);
            var ackWait = TimeSpan.FromSeconds(10);
            var ackPolicy = ConsumerConfigAckPolicy.Explicit;
            var consumer = await streamItems.Stream.CreateOrUpdateConsumerAsync(new ConsumerConfig(consumerName)
            {
                AckPolicy = ackPolicy,
                AckWait = ackWait,
                MaxWaiting = maxWaiting,
                MaxAckPending = 1,
                FilterSubject = filteredSubject
            });
            _logger.LogInformation("{0} Stream: {1} Consumer: {2} No. pending: {3}",
                ThreadIdString, consumer.Info.StreamName, consumer.Info.Name, consumer.Info.NumPending);
            return consumer;
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Publish a measurement without stream support
    /// </summary>
    /// <param name="connection">nats connection</param>
    /// <param name="subject">subject e.g. measurements.new.deviceX</param>
    /// <param name="measurement">measurement</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>void</returns>
    public async Task PublishAsync(NatsConnection connection, string subject, Measurement measurement)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            _logger.LogDebug("{0} NATS Publish: Subject: {1} Measurement: {2}",
                ThreadIdString, subject, measurement.ToLineProtocol(TimeResolution.s));
            var header = new NatsHeaders(_headerJson);
            await connection.PublishAsync(
                subject,
                measurement.ToJson(),
                header,
                cancellationToken: cts.Token);
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Publish a measurement WITH stream support
    /// </summary>
    /// <param name="context"></param>
    /// <param name="subject">subject e.g. measurements.new.deviceX</param>
    /// <param name="measurement">measurement</param>
    /// <exception cref="NatsException">NatsException</exception>
    /// <returns>void</returns>
    public async Task PublishAsync(INatsJSContext context, string subject, Measurement measurement)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            _logger.LogInformation("{0} NATS Publish: Subject: '{1}' Measurement: {2}",
                ThreadIdString, subject, measurement.ToLineProtocol(TimeResolution.s));
            var header = new NatsHeaders(_headerJson);
            var ackResponse =  await context.PublishAsync(
                subject: subject,
                data: measurement.ToJson(),
                headers: header,
                cancellationToken: cts.Token);
            ackResponse.EnsureSuccess();
            _logger.LogInformation("{0} NATS Publish ackknowledge: Stream: {1} Sequence: {2}",
                ThreadIdString, ackResponse.Stream, ackResponse.Seq);
        }
        catch (NatsException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // unexpected exception
            _logger.LogCritical(ex, ex.Message);
            throw;
        }
    }
}
