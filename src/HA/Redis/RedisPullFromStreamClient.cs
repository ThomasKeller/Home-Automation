namespace HA.Redis;

public class RedisPullFromStreamClient : RedisBaseClient
{
    //public string GroupName { get; set; } = Environment.MachineName;

    public RedisPullFromStreamClient(string configString) : base(configString)
    {
    }

    public RedisPullFromStreamClient(string host, int port = 6379) : base(host, port)
    {
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="count"></param>
    /// <param name="from"><millisecondsTime>-<sequenceNumber></param>
    /// <returns></returns>
    public IEnumerable<RedisStreamItem<Measurement>> ReadMeasurementsFromStream(int count, string from = "0-0")
    {
        var entries = GetDatabase().StreamRead(StreamName, from, count);//   .StreamRange(StreamName, from, null, count);
        foreach (var entry in entries)
        {
            var id = entry.Id.HasValue ? entry.Id.ToString() : string.Empty;
            var values = GetRedisValue(entry);
            var measurement = ConvertToMeasurement(values);
            if (measurement != null)
                yield return new RedisStreamItem<Measurement>(id, measurement);
        }
    }

    public IEnumerable<RedisStreamItem<string>> ReadStringFromStream(int count, string from = "0-0")
    {
        var items = new List<string>();
        var entries = GetDatabase().StreamRead(StreamName, from, count);
        foreach (var entry in entries)
        {
            var id = entry.Id.HasValue ? entry.Id.ToString() : string.Empty;
            var values = GetRedisValue(entry);
            var item = ConvertToString(values);
            if (item != null)
                yield return new RedisStreamItem<string>(id, item);
        }
    }

    /*public IEnumerable<Measurement> ReadMeasurementsFromGroup(int count, string from = "0-0")
    {
        CheckGroupExists(GroupName);
        var measurements = new List<Measurement>();
        var entries = GetDatabase().StreamReadGroup(
            StreamName, GroupName, CosumerName, from, count);
        if (entries.Length == 0)
            return measurements;
        foreach(var entry in entries)
        {
            var values = GetRedisValue(entry);
            var measurement = ConvertToMeasurement(values);
            if (measurement != null)
                measurements.Add(measurement);
        }
        return measurements;
    }

    public IEnumerable<string> ReadItemsFromGroup(int count, string from = "$")
    {
        CheckGroupExists(GroupName);
        var items = new List<string>();
        var entries = GetDatabase().StreamReadGroup(
            StreamName, GroupName, CosumerName, from, count);
        if (entries.Length == 0)
            return items;
        foreach (var entry in entries)
        {
            var values = GetRedisValue(entry);
            var item = ConvertToString(values);
            if (item != null)
                items.Add(item);
        }
        return items;
    }*/
}