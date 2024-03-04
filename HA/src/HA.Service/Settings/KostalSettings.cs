using HA.Service.Settings;
using Microsoft.Extensions.Configuration;

namespace HA.Kostal;

public class KostalSettings : AppSettingsBase
{
    public KostalSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }

    [EnvParameter("KOSTAL_URL")]
    [ConfigParameter("kostal", "url", Required = true)]
    public string KostalUrl { get; set; } = "http://192.168.x.x";

    [EnvParameter("KOSTAL_USER")]
    [ConfigParameter("kostal", "user", Required = true)]
    public string KostalUser { get; set; } = "pvserver";

    [EnvParameter("KOSTAL_PASSWORD")]
    [ConfigParameter("kostal", "password", Required = true)]
    public string KostalPassword { get; set; } = string.Empty;

    [EnvParameter("KOSTAL_MEASURE_INTERVAL_SEC")]
    [ConfigParameter("kostal", "measureIntervalSec")]
    public int MeasureInterval_s { get; set; } = 30;

    [EnvParameter("KOSTAL_STOP_DURING_SUNSET")]
    [ConfigParameter("kostal", "stopDuringSunset")]
    public bool KostalStopDuringSunset { get; set; } = true;

    [EnvParameter("KOSTAL_SLEEP_INTERVAL_MIN")]
    [ConfigParameter("kostal", "sleepIntervalMinutes")]
    public int SleepInterval_min { get; set; } = 10;

    [EnvParameter("LATITUDE")]
    [ConfigParameter("kostal", "latitude")]
    public double Latitude { get; set; } = 51.1853900;

    [EnvParameter("LONGTITUDE")]
    [ConfigParameter("kostal", "longtitude")]
    public double Longtitude { get; set; } = 6.4417200;

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
