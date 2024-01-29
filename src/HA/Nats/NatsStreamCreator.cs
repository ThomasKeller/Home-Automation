using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Client.JetStream;

namespace HA.Nats;

public class NatsStreamCreator
{
    private readonly ILogger _logger;

    public NatsConnection? Connection { get; private set; }

    public NatsJSContext? Context { get; private set; }

    public INatsJSStream? Stream { get; private set; }

    public string Subject { get; set; } = "measurements.>";

    public string StreamName { get; set; } = "MEASUREMENTS";

    public NatsStreamCreator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CreateStreamAsync(NatsOpts natsOpts, uint maxAgeInDays = 14)
    {
        try
        {
            Connection = new NatsConnection(natsOpts);
            var timeResponse = await Connection.PingAsync();
            var serverInfo = Connection.ServerInfo;
            _logger.LogInformation("NATS connection state: {0} Ping/Pong time: {1} ms", 
                Connection.ConnectionState, timeResponse.TotalMilliseconds);
            if (serverInfo != null)
            {
                _logger.LogInformation($"Server: Name: {serverInfo.Name} Version: {serverInfo.Version} Jetstream Enable: {serverInfo.JetStreamAvailable}");
                _logger.LogInformation($"Server: Host: {serverInfo.Host} Port: {serverInfo.Port} Id: {serverInfo.Id}");
            }
            Context = new NatsJSContext(Connection);
            var streamExists = await Context.CheckStreamExistAsync(StreamName); 
            _logger.LogInformation($"Stream '{StreamName}' exists: {streamExists}");
            Stream = streamExists
                ? await Context.GetStreamAsync(StreamName)
                : await Context.CreateStreamAsync(new StreamConfig(StreamName, new[] { Subject }) {
                    Retention = StreamConfigRetention.Workqueue,
                    MaxAge = TimeSpan.FromDays(maxAgeInDays) });
            var streamInfo = Stream.Info;
            if (streamInfo != null)
            {
                _logger.LogInformation("Stream Cluster: {0}", streamInfo.Cluster?.ToString());
                _logger.LogInformation("Stream Name: {0} Subject: {1}",
                    streamInfo.Config.Name, streamInfo.Config.Subjects.FirstOrDefault());
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex.Message);
        }
        return false;
    }
} 