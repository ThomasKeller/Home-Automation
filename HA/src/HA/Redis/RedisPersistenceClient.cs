using StackExchange.Redis;
using System.Reflection;

namespace HA.Redis;

public class RedisPersistenceClient
{
    private const string _cLineprotocol = "lineprotocol";
    private const string _cMimeType = "mimetpye";
    private static string _measurementType = new Measurement().GetType().FullName ?? "HA.Common.Measurement";
    private readonly ConfigurationOptions _configurationOptions;
    private ConnectionMultiplexer? _redis;
    private IDatabase? _database;
    private StreamGroupInfo? _streamGroupInfo;

    public string ClientName { get; set; } = CreateUniqueClientName();

    public string StreamName { get; set; } = "Measurements";

    public string GroupName { get; set; } = GetMachineName();

    public string CosumerName { get; set; } = GetAppName();

    public RedisPersistenceClient(string configString)
    {
        _configurationOptions = ConfigurationOptions.Parse(configString);
        _configurationOptions.ClientName = ClientName;
    }

    public RedisPersistenceClient(string host, int port = 6379)
    {
        var config = new ConfigurationOptions();
        config.SslHost = $"{host}:{port}";
        config.ClientName = ClientName;
        _configurationOptions = config;
    }

    public bool AddMeasurement(Measurement measurement)
    {
        var redisValue = GetDatabase().StreamAdd(StreamName, CreateValueEntry(measurement));
        return redisValue.HasValue;
    }

    public IEnumerable<Measurement> ReadFromGroup(int count)
    {
        CheckGroupExists();
        var measurements = new List<Measurement>();
        var entries = GetDatabase().StreamReadGroup(
            StreamName, GroupName, CosumerName, "$", count);
        if (entries.Length == 0)
            return measurements;
        foreach (var entry in entries)
        {
            var values = GetRedisValue(entry);
            var measurement = ConvertToMeasurement(values);
            if (measurement != null)
                measurements.Add(measurement);
        }
        return measurements;
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

    private IDatabase GetDatabase()
    {
        if (_database == null)
        {
            if (_redis == null)
                _redis = ConnectionMultiplexer.Connect(_configurationOptions.ToString());
            _database = _redis.GetDatabase();
        }
        return _database ?? throw new Exception("database is null");
    }

    private void CheckGroupExists()
    {
        if (string.IsNullOrEmpty(_streamGroupInfo?.Name) ||
            _streamGroupInfo?.Name != GroupName)
        {
            // create a group
            _streamGroupInfo = ReadStreamGroupInfo();
            if (string.IsNullOrEmpty(_streamGroupInfo?.Name))
            {
                if (!GetDatabase().StreamCreateConsumerGroup(StreamName, GroupName, "$"))
                {
                    throw new Exception($"Cannot create group '{GroupName}' for stream '{StreamName}'");
                }
                _streamGroupInfo = ReadStreamGroupInfo();
            }
        }
    }

    private NameValueEntry[] CreateValueEntry(Measurement measurement)
    {
        return new NameValueEntry[]
        {
            new NameValueEntry("type", measurement.GetType().FullName),
            new NameValueEntry(_cMimeType, _cLineprotocol),
            new NameValueEntry(_cLineprotocol, measurement.ToLineProtocol())
        };
    }

    private Dictionary<string, RedisValue> GetRedisValue(StreamEntry entry)
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

    private Measurement? ConvertToMeasurement(Dictionary<string, RedisValue> values)
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

    private StreamGroupInfo ReadStreamGroupInfo()
    {
        var streamGroupInfos = GetDatabase().StreamGroupInfo(StreamName, CommandFlags.None);
        return streamGroupInfos.FirstOrDefault(sgi => sgi.Name.Equals(GroupName));
    }

    private static string CreateUniqueClientName()
    {
        var machineName = Environment.MachineName;
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        return $"{machineName}-{assemblyName}";
    }

    private static string GetMachineName()
    {
        return Environment.MachineName;
    }

    private static string GetAppName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name ?? "UnknownAppName";
    }
}