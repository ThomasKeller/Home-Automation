using HA.Nats;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace HA.Tests;

public class NatsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public async Task test_that_nats_connection_is_created()
    {
        var url = "127.0.0.1:4222";
        var natsOpts = new NatsOpts
        {
            Url = url,
            Name = "NATS-Test-Producer",
            AuthOpts = new NatsAuthOpts { Username = "thomas", Password = "keller" }
        };
        var utils = new NatsUtils(TestLogger.Create<NatsTests>());
        var connection = await utils.CreateConnectionAsync(natsOpts);
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection.ConnectionState, Is.EqualTo(NatsConnectionState.Open));
        await Task.CompletedTask;
    }

    [Test]
    public async Task test_that_nats_stream_is_created_and_deleted_succesfully()
    {
        var url = "127.0.0.1:4222";
        var natsOpts = new NatsOpts
        {
            Url = url,
            Name = "NATS-Test-Producer",
            AuthOpts = new NatsAuthOpts { Username = "thomas", Password = "keller" }
        };
        var utils = new NatsUtils(TestLogger.Create<NatsTests>());
        var connection = await utils.CreateConnectionAsync(natsOpts);
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection.ConnectionState, Is.EqualTo(NatsConnectionState.Open));
        var streamInfo = await utils.CreateStreamAsync(connection, "TEST", "test.>", 1);
        Assert.That(streamInfo, Is.Not.Null);
        Assert.That(streamInfo.Connection, Is.Not.Null);
        Assert.That(streamInfo.Stream, Is.Not.Null);
        Assert.That(streamInfo.Context, Is.Not.Null);
        var isAvailable = await utils.CheckStreamExistAsync(streamInfo.Context, "TEST");
        Assert.That(isAvailable, Is.True);

        var consumer = await utils.CreateOrUpdateConsumerAsync(connection, "InfluxStore", "TEST", "test.>");
        Assert.That(consumer, Is.Not.Null);

        var deleted = await utils.DeleteStreamAsync(connection, "TEST");
        Assert.That(deleted, Is.True);
        isAvailable = await utils.CheckStreamExistAsync(streamInfo.Context, "TEST");
        Assert.That(isAvailable, Is.False);
        await Task.CompletedTask;
    }



    [Test]
    public void Test2()
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
        var sut = new NatsPublisher(TestLogger.Create<NatsAuthOpts>(), natsOpts, true);
        await sut.PublishAsync("health.test.value1", "Hello1");
        await sut.PublishAsync("health.test.value2", "Hello2");
        await sut.PublishAsync("health.test.value3", "Hello3");
        await sut.PublishAsync("health.test.value4", "Hello4");
        await sut.PublishAsync("health.test.value5", "Hello5");
        Thread.Sleep(1000);
    }
}