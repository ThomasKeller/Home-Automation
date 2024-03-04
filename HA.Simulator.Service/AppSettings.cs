using HA.Service.Settings;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace HA.Simulator.Service;

public class AppSettings 
{
    public AppInitSettings Application { get; private set; }

    public NatsSettings Nats { get; private set; }

    public NatsStreamSettings NatsStream { get; private set; }

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings;
        Nats = new NatsSettings(Application.Configuration);
        NatsStream = new NatsStreamSettings(Application.Configuration);
    }

    public void CheckSettings()
    {
        Application.CheckSettings();
        Nats.CheckSettings();
        NatsStream.CheckSettings();
    }
}