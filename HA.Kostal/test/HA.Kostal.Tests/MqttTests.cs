using HA.Mqtt;

namespace HA.Kostal.Tests;

public class MqttTests
{
    private string _mqttHost = $"192.168.111.50";

    [SetUp]
    public void Setup()
    {
    }

    [TearDown]
    public void Teardown()
    {
    }


    [Test]
    public async Task publish_payload_to_MQTT_topic_successfully_Async()
    {
        var mqttClient = new MqttPublisher(TestLogger.Create<MqttPublisher>(), _mqttHost);

        await mqttClient.PublishAsync("measurements/test/value1", "12");
        await mqttClient.DisconnectAsync();
        Assert.Pass();
    }
}