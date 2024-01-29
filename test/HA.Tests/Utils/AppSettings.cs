using HA.AppTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;

namespace HA.Tests.Utils;

public class AppSettings : AppSettingsBase
{
    public AppSettings(ILogger logger)
    {
        ReadEnvironmentVariables();
    }

    [EnvParameter("ENV_STRING")]
    [ConfigParameter("env1", "var1", Required = true)]
    public string? EnvString { get; set; }

    [EnvParameter("ENV_INT")]
    [ConfigParameter("env1", "var2")]
    public int EnvInt32 { get; set; }

    [EnvParameter("ENV_BOOL")]
    public bool EnvBool { get; set; }

    [EnvParameter("ENV_DOUBLE")]
    public double EnvDouble { get; set; }

    [EnvParameter("KOSTAL_STOP_DURING_SUNSET")]
    [ConfigParameter("kostal", "stopDuringSunset")]
    public bool KostalStopDuringSunset { get; set; } = true;

    public void ReadConfigFile(string appConfigFilePath)
    {
        var configurationBuilder = new ConfigurationBuilder();
        IConfiguration conf = configurationBuilder
            .AddJsonFile(appConfigFilePath)
            .Build();
        ReadAppConfigFile(conf);
    }
}
