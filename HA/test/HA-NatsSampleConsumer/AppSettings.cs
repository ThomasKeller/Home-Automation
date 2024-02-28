using HA.Service.Settings;
using Microsoft.Extensions.Logging;

public class AppSettings
{
    public AppInitSettings Application { get; private set; }

    public NatsSettings Nats { get; private set; }

    public NatsConsumerSettings NatsConsumer { get; private set; }

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings;
        Nats = new NatsSettings(Application.Configuration);
        NatsConsumer = new NatsConsumerSettings(Application.Configuration);
    }

    public void CheckSettings()
    {
        Application.CheckSettings();
        Nats.CheckSettings();
        NatsConsumer.CheckSettings();
    }
}