using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HA.Redis;

public class RedisPushToStreamClient : RedisBaseClient, IRedisBaseClient, IRedisPushToStreamClient
{
    private readonly ILogger _logger;
    private readonly IFileStore _fileStore;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private Task? _task;

    public TimeSpan RetryAfter { get; set; } = TimeSpan.FromMinutes(5);

    public RedisPushToStreamClient(ILogger<RedisPushToStreamClient> logger, IFileStore fileStore, string configString)
        : base(configString)
    {
        _logger = logger;
        _fileStore = fileStore;
    }

    public RedisPushToStreamClient(ILogger<RedisPushToStreamClient> logger, IFileStore fileStore, string host, int port = 6379)
        : base(host, port)
    {
        _logger = logger;
        _fileStore = fileStore;
    }

    public bool PushToStream(Measurement measurement)
    {
        if (IsInErrorState())
        {
            _logger.LogWarning("In Error State: Write Measurement to file");
            _fileStore.WriteToFile(measurement.ToJson());
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
            _fileStore.WriteToFile(measurement.ToJson());
            return false;
        }
    }

    public bool PushToStream(string item)
    {
        if (IsInErrorState())
        {
            _logger.LogWarning("In Error State: Write string to file");
            _fileStore.WriteToFile(item);
            return false;
        }
        try
        {
            if (item == null)
                return false;
            _logger.LogDebug("Add string to Redis stream.");
            var redisValue = GetDatabase().StreamAdd(StreamName, CreateValueEntry(item));
            return redisValue.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            _lastErrorTime = DateTime.Now;
            _fileStore.WriteToFile(item);
            return false;
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
                var fileContent = _fileStore.ReadFirstFile();
                while (fileContent.Lines != null)
                {
                    _logger.LogInformation($"Write cached file lines to Redis. Count: {fileContent.Lines.Count}");
                    foreach (var item in fileContent.Lines)
                    {
                        if (!PushToStream(item))
                        {
                            _logger.LogError("Cannot push line to Redis");
                            throw new Exception("Cannot push line to Redis");
                        }
                    }
                    if (fileContent.FileInfo != null)
                    {
                        var renamed = _fileStore.MarkAsProcessed(fileContent.FileInfo);
                        _logger.LogInformation($"Mark file as proccessed: '{fileContent.FileInfo.FullName}'");
                    }
                    fileContent = _fileStore.ReadFirstFile();
                    if (fileContent.FileCount == 0)
                        break;
                }
            });
        }
    }

    protected override void OnRedisConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        base.OnRedisConnectionFailed(sender, e);
        _logger.LogError($"Redis connection failed {e.ToString()}");
    }

    //protected void OnRedisConnectionRestored()

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