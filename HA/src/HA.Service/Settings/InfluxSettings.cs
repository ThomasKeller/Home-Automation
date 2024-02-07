using Microsoft.Extensions.Configuration;

namespace HA.Service.Settings;

public class InfluxSettings : AppSettingsBase
{
    public InfluxSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }

    [EnvParameter("INFLUX_URL")]
    [ConfigParameter("influxDB", "url", Required = true)]
    public string InfluxUrl { get; set; } = "http://x.x.x.x:8086/";

    [EnvParameter("INFLUX_ORG")]
    [ConfigParameter("influxDB", "org", Required = true)]
    public string InfluxOrg { get; set; } = "Org";

    [EnvParameter("INFLUX_BUCKET")]
    [ConfigParameter("influxDB", "bucket", Required = true)]
    public string InfluxBucket { get; set; } = "Bucket";

    [EnvParameter("INFLUX_TOKEN")]
    [ConfigParameter("influxDB", "token", Required = true)]
    public string? InfluxToken { get; set; }

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
