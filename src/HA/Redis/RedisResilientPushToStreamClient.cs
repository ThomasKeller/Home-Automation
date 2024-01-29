using HA.Store;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HA.Redis;

public class RedisResilientPushToStreamClient : RedisBaseClient, IRedisBaseClient, IRedisPushToStreamClient
{
    private readonly ILogger _logger;
    private readonly IMeasurementStore _store;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private Task? _task;

    public TimeSpan RetryAfter { get; set; } = TimeSpan.FromMinutes(5);

    public RedisResilientPushToStreamClient(ILogger logger, IMeasurementStore store, string configString)
        : base(configString)
    {
        _logger = logger;
        _store = store;
    }

    public RedisResilientPushToStreamClient(ILogger logger, IMeasurementStore store, string host, int port = 6379)
        : base(host, port)
    {
        _logger = logger;
        _store = store;
    }

    public bool PushToStream(Measurement measurement)
    {
        if (IsInErrorState())
        {
            _logger.LogWarning("In Error State: Write Measurement to store");
            _store.Save(measurement);
            return false;
        }
        try
        {
            if (measurement == null)
                return false;
            _logger.LogDebug("Add Measurement to Redis stream.");
            var redisValue = GetDatabase().StreamAdd(StreamName, CreateValueEntry(measurement));
            return redisValue.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            _lastErrorTime = DateTime.Now;
            _store.Save(measurement);
            return false;
        }
    }

    private void RemoveMeasurement(int? id)
    {
        if (id == null)
            return;
        try
        {
            var count = _store.Remove(id.Value);
            _logger.LogInformation("Remove measurement from store. Count {0} Id: {1}", count, id);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Cannot remove measurement from store: Id: {0}. Reason: {1}", id, ex.Message);
        }
    }

    protected override void OnRedisConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        base.OnRedisConnectionRestored(sender, e);
        _logger.LogInformation($"Redis connection restored {e.ToString()}");
        if (_task == null || _task.IsCompleted)
        {
            _task = Task.Run(() =>
            {
                if (_store.Count() > 0)
                {
                    foreach (var item in _store.GetAll())
                    {
                        if (PushToStream(item.ToMeasurement()))
                        {
                            RemoveMeasurement(item.Id);
                        }
                        else
                        {
                            _logger.LogError("Cannot push line to Redis");
                            throw new Exception("Cannot push line to Redis");
                        }
                    }
                }
            });
        }
    }

    protected override void OnRedisConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        base.OnRedisConnectionFailed(sender, e);
        _logger.LogError($"Redis connection failed {e.ToString()}");
    }

    private bool IsInErrorState()
    {
        if (IsConnected)
        {
            return false;
        }
        return _lastErrorTime > DateTime.MinValue &&
            DateTime.Now < _lastErrorTime + RetryAfter;
    }
}