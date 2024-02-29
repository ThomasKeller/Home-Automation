using HA.Service.Settings;
using Microsoft.Extensions.Logging;

namespace HA.MqttPublisher.Service;

public class AppSettings
{
    public AppInitSettings Application { get; private set; }

    public MqttSettings Mqtt { get; private set; }

    public NatsSettings Nats { get; private set; }

    public NatsConsumerSettings NatsConsumer { get; private set; }

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings;
        Mqtt = new MqttSettings(Application.Configuration);
        Nats = new NatsSettings(Application.Configuration);
        NatsConsumer = new NatsConsumerSettings(Application.Configuration);
    }

    public void CheckSettings()
    {
        Application.CheckSettings();
        Mqtt.CheckSettings();
        Nats.CheckSettings();
        NatsConsumer.CheckSettings();
    }
}