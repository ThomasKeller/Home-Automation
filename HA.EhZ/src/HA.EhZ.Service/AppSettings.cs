using HA.AppTools;
using HA.Influx;
using HA.Mqtt;

namespace HA.EhZ.Service;

public class AppSettings 
{
    public AppInitSettings Application { get; private set; }

    public EhZSettings Ehz { get; private set; }

    public InfluxSettings Influx { get; private set; }

    public MqttSettings Mqtt { get; private set; }

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings;
        Ehz = new EhZSettings(Application.Configuration);
        Influx = new InfluxSettings(Application.Configuration);
        Mqtt = new MqttSettings(Application.Configuration);
    }

    public void CheckSettings()
    {
        Application.CheckSettings();
        Influx.CheckSettings();
        Mqtt.CheckSettings();
    }

    public int UdpPort { get; set; } = 5557;
}