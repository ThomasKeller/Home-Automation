using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace HA.Nats;

public class NatsPublisher
{
    private static readonly Dictionary<string, StringValues> _headerJson = new() { { "PayloadType", "JSON" }, { "DataType", typeof(Measurement).FullName } };
    private readonly ILogger _logger;
    private readonly NatsOpts _natsOpts;
    private NatsConnection? _connection;
    private INatsJSContext? _context;
    private bool _useStreamContext;

    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public NatsPublisher(ILogger logger, NatsOpts natsOpts, bool useStreamContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _natsOpts = natsOpts ?? throw new ArgumentNullException(nameof(natsOpts));
        _useStreamContext = useStreamContext;
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (_connection == null)
        {
            await InitAsync();
        }
        return _connection != null && _connection.ConnectionState == NatsConnectionState.Open;
    }

    public async Task PublishAsync(string subject, string? payload)
    {
        if (_connection == null) {
            await InitAsync(); 
        }
        if (payload != null && payload.Length > 0)
        {
            _logger.LogDebug("{0} NATS Publish: Subject: {1} Payload Length: {2}",
                ThreadIdString, subject, payload.Length);
            if (_connection != null)
            {
                if (_context != null && _useStreamContext)
                {
                    var response = await _context.PublishAsync(
                        subject, data: payload);
                    _logger.LogDebug("{0} Reponse: {1}", ThreadIdString, response);
                }
                else
                {
                    await _connection.PublishAsync(
                        subject, data: payload);
                }
            }
        }
    }

    public async Task PublishAsync(string subject, Measurement measurement)
    {
        if (_connection == null)
        {
            await InitAsync();
        }
        if (measurement != null)
        {
            _logger.LogDebug("{0} NATS Publish: Subject: {1} Measurement: {2}", 
                ThreadIdString, subject, measurement.ToLineProtocol(TimeResolution.s));
            if (_connection != null)
            {
                var header = new NatsHeaders(_headerJson);
                if (_context != null && _useStreamContext)
                {
                    var response = await _context.PublishAsync(
                        subject,
                        data: measurement.ToJson(),
                        headers: header);
                    _logger.LogDebug("{0} Reponse: {1}", ThreadIdString, response);
                }
                else
                {
                    await _connection.PublishAsync(
                        subject,
                        data: measurement.ToJson(),
                        header);
                }
            }
        }
    }

    private async Task<NatsConnectionState> InitAsync()
    {
        _connection = new NatsConnection(_natsOpts);
        var timeResponse = await _connection.PingAsync();
        var serverInfo = _connection.ServerInfo;
        _logger.LogInformation("{0} NATS connection state: {1} Ping/Pong time: {2} ms",
            ThreadIdString, _connection.ConnectionState, timeResponse.TotalMilliseconds);
        if (serverInfo != null)
        {
            _logger.LogInformation("{0} Server: Name: {1} Version: {2} Jetstream Enable: {3}",
                ThreadIdString, serverInfo.Name, serverInfo.Version, serverInfo.JetStreamAvailable);
            _logger.LogInformation("{0} Server: Host: {1} Port: {2} Id: {3}",
                ThreadIdString, serverInfo.Host, serverInfo.Port, serverInfo.Id);
            if (serverInfo.JetStreamAvailable)
                _context = new NatsJSContext(_connection);
        }
        return _connection.ConnectionState;
    }
}
