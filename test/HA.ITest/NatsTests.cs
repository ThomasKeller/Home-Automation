using HA.Nats;
using Microsoft.Extensions.Logging;

namespace HA.ITest;

public class NatsTests
{
    private ILoggerFactory _loggerFactory;
    private string __natsServer = "nats://127.0.0.1:4222";

    [SetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("ha", LogLevel.Debug)
                .AddConsole());
    }

    [Test]
    public void Test1()
    {
        var puplisher = new NatsPublisher(_loggerFactory.CreateLogger<NatsPublisher>());
        var consumer = new NatsConsumer(_loggerFactory.CreateLogger<NatsConsumer>());
        var publishedMsg1 = "mypayload1";
        var publishedMsg2 = "mypayload2";
        
        consumer.Subscribe("ha.test1");

        puplisher.Publish("ha.test1", publishedMsg1);
        puplisher.Publish("ha.test1", publishedMsg2);

        var msg1 = consumer.NextStringMessage();
        var msg2 = consumer.NextStringMessage();

        Assert.That(publishedMsg1, Is.EqualTo(msg1));
        Assert.That(publishedMsg2, Is.EqualTo(msg2));
    }
}