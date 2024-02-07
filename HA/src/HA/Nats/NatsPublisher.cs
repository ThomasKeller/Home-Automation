using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;

namespace HA.Nats;

public class NatsPublisher
{
    private static readonly Dictionary<string, StringValues> _headerLP = new Dictionary<string, StringValues> 
                    { { "PayloadType", "LineProtocol" }, { "DataType", typeof(Measurement).FullName } };
    private static readonly Dictionary<string, StringValues> _headerJson = new Dictionary<string, StringValues> 
                    { { "PayloadType", "JSON" }, { "DataType", typeof(Measurement).FullName } };
    private readonly ILogger _logger;
    private readonly NatsOpts _natsOpts;
    private NatsConnection? _connection;

    public NatsPublisher(ILogger logger, NatsOpts natsOpts)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _natsOpts = natsOpts ?? throw new ArgumentNullException(nameof(natsOpts));
    }

    public NatsPublisher(ILogger logger, NatsOptions natsOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (natsOptions == null) throw new ArgumentNullException(nameof(natsOptions));
        _natsOpts = new NatsOpts
        {
            Url = natsOptions.Url,
            Name = natsOptions.ClientName,
            AuthOpts = new NatsAuthOpts
            {
                Username = natsOptions.User,
                Password = natsOptions.Password
            }
        };
    }

    public async Task<bool> IsConnectedAsync()
    {
        var connectionsState = await InitAsync();
        return connectionsState == NatsConnectionState.Open; 
    }

    public async Task PublishAsync(string subject, string? payload)
    {
        if (_connection == null) {
            await InitAsync(); 
        }
        if (payload != null && payload.Length > 0)
        {
            _logger.LogDebug("NATS Publish: Subject: {0} Payload Length: {1}", subject, payload.Length);
            if (_connection != null)
            {
                await _connection.PublishAsync(subject, payload);
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
            _logger.LogDebug("NATS Publish: Subject: {0} Measurement: {1}", subject, measurement.ToLineProtocol(TimeResolution.s));
            if (_connection != null)
            {
                var headerParams = lineProtocol ? _headerLP : _headerJson;
                var header = new NatsHeaders(headerParams);
                await _connection.PublishAsync(
                    subject, 
                    lineProtocol ? measurement.ToLineProtocol(TimeResolution.ms) : measurement.ToJson(), 
                    header);
            }
        }
    }

    private async Task<NatsConnectionState> InitAsync()
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
        return _connection.ConnectionState;
    }
}
