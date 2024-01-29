using StackExchange.Redis;

namespace HA.Redis;

public interface IRedisBaseClient
{
    string ClientName { get; set; }
    string CosumerName { get; set; }
    RedisBaseClient.ConnectStatus Status { get; }
    string StreamName { get; set; }

    bool DeleteKey(string key);

    StreamInfo GetMeasurmentStreamInfo();

    string? GetStringValue(string key);

    bool SetStringValue(string key, string value);

    long StreamLength();

    long StreamTrim(int maxLength);
}