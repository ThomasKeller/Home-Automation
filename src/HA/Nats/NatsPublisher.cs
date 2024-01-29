using Microsoft.Extensions.Logging;
using NATS.Client;
using System.Text;

namespace HA.Nats;

public class NatsPublisher
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _connectionFactory = new ConnectionFactory();
    private Options _options;
    private IConnection _connection;
    //private IJetStream _jetStream;

    public NatsPublisher(ILogger logger, string natsUrl = "nats://127.0.0.1:4222")
    {
        _logger = logger;
        _options = ConnectionFactory.GetDefaultOptions();
        _options.Url= natsUrl;
        _connection = _connectionFactory.CreateConnection();
        /*var opts = new NatsOpts
        {
            Url = url,
            LoggerFactory = loggerFactory,
            Name = "NATS-by-Example",
        };
        await using var nats = new NatsConnection(opts);*/


        //_jetStream =  _connection.CreateJetStreamContext(JetStreamOptions.DefaultJsOptions);
    }

    public void Publish(string subject, string? payload)
    {
        if (payload != null && payload.Length > 0)
        {
            _logger.LogDebug("NATS Publish: Subject: {0} Payload Length: {1}", subject, payload.Length);
            var header = new MsgHeader();
            header.Set("time", DateTime.UtcNow.Ticks.ToString());
            var msg = new Msg(subject, header, Encoding.UTF8.GetBytes(payload));
            _connection.Publish(msg);
        }
    }
}
