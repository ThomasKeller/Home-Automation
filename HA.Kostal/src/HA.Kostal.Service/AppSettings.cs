using HA.Service.Settings;

namespace HA.Kostal.Service;

public class AppSettings
{
    public AppInitSettings Application { get; private set; }

    public KostalSettings Kostal { get; private set; }

    public NatsSettings Nats {  get; private set; }

    public NatsStreamSettings NatsStream { get; private set; }

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings;
        Kostal = new KostalSettings(Application.Configuration);
        Nats = new NatsSettings(Application.Configuration);
        NatsStream = new NatsStreamSettings(Application.Configuration);
    }

    public void CheckSettings()
    {
        Application.CheckSettings();
        Kostal.CheckSettings();
        Nats.CheckSettings();
        NatsStream.CheckSettings();
    }
}