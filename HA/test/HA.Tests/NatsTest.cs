using HA.Nats;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace HA.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var url = "192.168.111.49:4222";
        var natsOpts = new NatsOpts {
            Url = url,
            Name = "NATS-Test-Producer",
            AuthOpts = new NatsAuthOpts { Username = "nats", Password = "transfer" }
        };
        var consumer = new QueueConsumerWorker(NullLogger.Instance, natsOpts);
        consumer.Subject = "measurements.new";
        consumer.ConsumerName = "NatsTest";
        consumer.StartAsync(new CancellationTokenSource(50000).Token);

        Thread.Sleep(90000);
        Assert.Pass();
    }
}