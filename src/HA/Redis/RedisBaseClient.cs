using StackExchange.Redis;
using System.Reflection;

namespace HA.Redis;

public abstract class RedisBaseClient : IRedisBaseClient
{
    public enum ConnectStatus
    {
        Init,
        Connected,
        LostConnetion
    }

    protected const string _cLineprotocol = "lineprotocol";
    protected const string _cMimeType = "mimetpye";
    protected const string _cStringType = "stringType";
    protected static string _measurementType = new Measurement().GetType().FullName ?? "HA.Common.Measurement";
    private readonly ConfigurationOptions _configurationOptions;
    private ConnectionMultiplexer? _redis;
    private IDatabase? _database;
    private StreamGroupInfo? _streamGroupInfo;

    public ConnectStatus Status { get; private set; } = ConnectStatus.Init;

    public string ClientName { get; set; } = CreateUniqueClientName();

    public string StreamName { get; set; } = "Measurements";

    //public string GroupName { get; set; } = GetMachineName();

    public string CosumerName { get; set; } = GetAppName();

    public bool IsConnected => _redis?.IsConnected ?? false;

    public bool IsConnecting => _redis?.IsConnecting ?? false;

    public RedisBaseClient(string configString)
    {
        _configurationOptions = ConfigurationOptions.Parse(configString);
        _configurationOptions.ClientName = ClientName;
    }

    public RedisBaseClient(string host, int port = 6379) : this($"{host}:{port}")
    {
    }

    public StreamInfo GetMeasurmentStreamInfo()
    {
        return GetDatabase().StreamInfo(StreamName);
    }

    public long StreamLength()
    {
        return _database != null
            ? _database.StreamLength(StreamName)
            : 0;
    }

    public long StreamTrim(int maxLength)
    {
        return _database != null
            ? _database.StreamTrim(StreamName, maxLength)
            : 0;
    }

    public bool SetStringValue(string key, string value)
    {
        return GetDatabase().StringSet(key, value);
    }

    public string? GetStringValue(string key)
    {
        var value = GetDatabase().StringGet(key);
        return value.HasValue ? value.ToString() : null;
    }

    public bool DeleteKey(string key)
    {
        return GetDatabase().KeyDelete(key);
    }

    protected ConnectionMultiplexer GetRedis()
    {
        if (_redis == null)
        {
            _redis = ConnectionMultiplexer.Connect(_configurationOptions.ToString());
            _redis.ConnectionFailed += OnRedisConnectionFailed;
            _redis.ConnectionRestored += OnRedisConnectionRestored;
            Status = ConnectStatus.Connected;
        }
        return _redis;
    }

    protected IDatabase GetDatabase()
    {
        if (_database == null)
        {
            GetRedis();
            _database = _redis?.GetDatabase();
        }
        return _database ?? throw new RedisConnectionException(ConnectionFailureType.InternalFailure, "database is null");
    }

    protected virtual void OnRedisConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        Status = ConnectStatus.Connected;
    }

    protected virtual void OnRedisConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        Status = ConnectStatus.LostConnetion;
    }

    protected void CheckGroupExists(string groupName)
    {
        if (string.IsNullOrEmpty(_streamGroupInfo?.Name) ||
            _streamGroupInfo?.Name != groupName)
        {
            // create a group
            _streamGroupInfo = ReadStreamGroupInfo(groupName);
            if (string.IsNullOrEmpty(_streamGroupInfo?.Name))
            {
                if (!GetDatabase().StreamCreateConsumerGroup(StreamName, groupName))
                {
                    throw new Exception($"Cannot create group '{groupName}' for stream '{StreamName}'");
                }
                _streamGroupInfo = ReadStreamGroupInfo(groupName);
            }
        }
    }

    protected NameValueEntry[] CreateValueEntry(Measurement measurement)
    {
        return new NameValueEntry[]
        {
            new NameValueEntry("type", measurement.GetType().FullName),
            new NameValueEntry(_cMimeType, _cLineprotocol),
            new NameValueEntry(_cLineprotocol, measurement.ToLineProtocol())
        };
    }

    protected NameValueEntry[] CreateValueEntry(string item)
    {
        return new NameValueEntry[]
        {
            new NameValueEntry("type", item.GetType().FullName),
            new NameValueEntry(_cMimeType, _cStringType),
            new NameValueEntry(_cStringType, item)
        };
    }

    protected Dictionary<string, RedisValue> GetRedisValue(StreamEntry entry)
    {
        var result = new Dictionary<string, RedisValue>();
        if (!string.IsNullOrEmpty(entry.Id)
            && !entry.IsNull
            && entry.Values.Length > 0)
        {
            foreach (var value in entry.Values)
                result.Add(value.Name.ToString(), value.Value);
        }
        return result;
    }

    protected Measurement? ConvertToMeasurement(Dictionary<string, RedisValue> values)
    {
        if (values.TryGetValue("type", out var typeRedisValue)
            && typeRedisValue.ToString() == _measurementType
            && values.TryGetValue("mimetpye", out var mimeTypeRedisValue))
        {
            var mimeType = mimeTypeRedisValue.ToString();
            if (mimeType == "lineprotocol")
            {
                var lineprotocol = values.TryGetValue(mimeType, out var lpRedisValue)
                    ? lpRedisValue.ToString() : string.Empty;
                return Measurement.FromLineProtocol(lineprotocol);
            }
        }
        return null;
    }

    protected string? ConvertToString(Dictionary<string, RedisValue> values)
    {
        if (values.TryGetValue("type", out var typeRedisValue)
            && typeRedisValue.ToString() == "System.String"
            && values.TryGetValue("mimetpye", out var mimeTypeRedisValue))
        {
            var mimeType = mimeTypeRedisValue.ToString();
            if (mimeType == _cStringType)
            {
                return values.TryGetValue(mimeType, out var lpRedisValue)
                    ? lpRedisValue.ToString() : string.Empty;
            }
        }
        return null;
    }

    protected StreamGroupInfo ReadStreamGroupInfo(string groupName)
    {
        var streamGroupInfos = GetDatabase().StreamGroupInfo(StreamName, CommandFlags.None);
        return streamGroupInfos.FirstOrDefault(sgi => sgi.Name.Equals(groupName));
    }

    protected static string CreateUniqueClientName()
    {
        var machineName = Environment.MachineName;
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        return $"{machineName}-{assemblyName}";
    }

    private static string GetAppName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name ?? "UnknownAppName";
    }
}