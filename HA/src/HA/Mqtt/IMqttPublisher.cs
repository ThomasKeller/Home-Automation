namespace HA.Mqtt;

public interface IMqttPublisher
{
    string? ClientId { get; }

    Task DisconnectAsync();

    Task<bool> PublishAsync(IDictionary<string, string> topicsAndpPayloads);

    Task<bool> PublishAsync(string topic, string payload);

    Task<bool> PublishAsync(Measurement measurement);
}