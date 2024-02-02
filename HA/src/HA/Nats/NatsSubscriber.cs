using HA.Observable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;

namespace HA.Nats;

public class NatsSubscriber : ObservableBase<Measurement>
{
    private static readonly Dictionary<string, StringValues> _headerLP = new Dictionary<string, StringValues>
                    { { "PayloadType", "LineProtocol" }, { "DataType", typeof(Measurement).FullName } };
    private static readonly Dictionary<string, StringValues> _headerJson = new Dictionary<string, StringValues>
                    { { "PayloadType", "JSON" }, { "DataType", typeof(Measurement).FullName } };
    private readonly ILogger _logger;
    private readonly NatsOpts _natsOpts;
    private NatsConnection? _connection;

    public NatsSubscriber(ILogger logger, NatsOpts natsOpts)
    {
        _logger = logger;
        _natsOpts = natsOpts;
    }

    public async Task SubscibeAsync(string subject, string? queueGroup = null)
    {
        if (_connection == null) {
            await InitAsync(); 
        }

        _logger.LogDebug("NATS Publish: Subject: {0} Query Group: {1}", subject, queueGroup);
        if (_connection != null)
        {
            await foreach(var msg in _connection.SubscribeAsync<string>(subject, queueGroup)) {
                if (msg.Data != null)
                {
                    try
                    {
                        if (msg.Data.StartsWith("{"))
                        {
                            var measurement = Measurement.FromJson(msg.Data);
                            if (measurement != null)
                                ExecuteOnNext(measurement);
                        }
                        else
                        {
                            var measurement = Measurement.FromLineProtocol(msg.Data);
                            if (measurement != null)
                                ExecuteOnNext(measurement);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExecuteOnError(ex);
                    }
                }
            } 
        }
    }

    public async Task PublishAsync(string subject, Measurement measurement, bool lineProtocol = false)
    {
        if (_connection == null)
        {
            await InitAsync();
        }
        if (measurement != null)
        {
            _logger.LogDebug("NATS Publish: Subject: {0} Measurement: {1}", 
                subject, measurement.ToLineProtocol(TimeResolution.s));
            if (_connection != null)
            {
                if (lineProtocol)
                    await _connection.PublishAsync(subject, measurement.ToLineProtocol(TimeResolution.ms), 
                        new NatsHeaders(_headerLP));
                else
                    await _connection.PublishAsync(subject, measurement.ToJson(), new NatsHeaders(_headerJson));
            }
        }
    }

    private async Task InitAsync()
    {
        _connection = new NatsConnection(_natsOpts);
        var timeResponse = await _connection.PingAsync();
        var serverInfo = _connection.ServerInfo;
        _logger.LogInformation("NATS connection state: {0} Ping/Pong time: {1} ms",
            _connection.ConnectionState, timeResponse.TotalMilliseconds);
        if (serverInfo != null)
        {
            _logger.LogInformation($"Server: Name: {serverInfo.Name} Version: {serverInfo.Version} Jetstream Enable: {serverInfo.JetStreamAvailable}");
            _logger.LogInformation($"Server: Host: {serverInfo.Host} Port: {serverInfo.Port} Id: {serverInfo.Id}");
        }
    }
}
