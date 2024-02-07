using HA.Service.Settings;

namespace HA.EhZ.Service;

public class AppSettings 
{
    public AppInitSettings Application { get; private set; }

    public EhZSettings Ehz { get; private set; }

    public InfluxSettings Influx { get; private set; }

    public MqttSettings Mqtt { get; private set; }

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings ?? throw new ArgumentNullException(nameof(appInitSettings));
        Ehz = new EhZSettings(Application.Configuration ?? throw new ArgumentNullException(nameof(appInitSettings)));
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