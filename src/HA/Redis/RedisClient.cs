using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HA.Redis;

public class RedisClient : RedisBaseClient
{
    private readonly ILogger _logger;

    public RedisClient(ILogger logger, string configString) : base(configString)
    {
        _logger = logger;
    }

    public RedisClient(ILogger logger, string host, int port = 6379) : base(host, port)
    {
        _logger = logger;
    }

    public ISubscriber Subscriber => GetDatabase().Multiplexer.GetSubscriber();

    protected override void OnRedisConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        var redis = GetRedis();
        _logger.LogWarning("Redis connection lost. Client: {0} Status: {1}", redis.ClientName, redis.GetStatus());
        base.OnRedisConnectionFailed(sender, e);
    }

    protected override void OnRedisConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        var redis = GetRedis();
        _logger.LogWarning("Redis connection restored. Client: {0} Status: {1}", redis.ClientName, redis.GetStatus());
        base.OnRedisConnectionRestored(sender, e);
    }
}