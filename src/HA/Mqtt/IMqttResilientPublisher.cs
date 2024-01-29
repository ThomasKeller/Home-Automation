namespace HA.Mqtt;

public interface IMqttResilientPublisher
{
    string? ClientId { get; }

    void Disconnect();

    void Publish(IEnumerable<Tuple<string, string>> topicsAndpPayloads);

    void Publish(string topic, string payload);

    void Publish(Measurement measurement);
}