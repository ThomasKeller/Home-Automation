using HA.Nats;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace HA.Tests;

public class Tests
{
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("ha", LogLevel.Debug));
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
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

    [Test]
    public void TestAsync()
    {
        AsyncHelper.RunSync(() => PublishtAsync());
        Assert.Pass();
    }

    public async Task PublishtAsync()
    {
        var url = "192.168.111.49:4222";
        var natsOpts = new NatsOpts
        {
            Url = url,
            Name = "NATS-Test-Producer",
            AuthOpts = new NatsAuthOpts { Username = "nats", Password = "transfer" }
        };
        var sut = new NatsPublisher(_loggerFactory.CreateLogger<NatsAuthOpts>(), natsOpts);
        await sut.PublishAsync("health.test.value1", "Hello1");
        await sut.PublishAsync("health.test.value2", "Hello2");
        await sut.PublishAsync("health.test.value3", "Hello3");
        await sut.PublishAsync("health.test.value4", "Hello4");
        await sut.PublishAsync("health.test.value5", "Hello5");
        Thread.Sleep(1000);
    }
}