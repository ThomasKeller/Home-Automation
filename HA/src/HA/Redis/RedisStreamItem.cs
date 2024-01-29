namespace HA.Redis;

public class RedisStreamItem<T>
{
    public RedisStreamItem()
    {
    }

    public RedisStreamItem(string? id, T? item)
    {
        Id = id;
        Item = item;
    }

    public string? Id { get; set; }
    public T? Item { get; set; }
}