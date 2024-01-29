namespace HA.Mqtt;

public interface IMqttConsumer
{
    string? ClientId { get; }

    Task<bool> SubscribeAsync(string topic);
}