using Microsoft.Extensions.Configuration;

namespace HA.Service.Settings;

public class RedisSettings : AppSettingsBase
{
    public RedisSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }

    [EnvParameter("REDIS_HOST")]
    [ConfigParameter("redis", "host", Required = true)]
    public string RedisHost { get; set; } = "localhost";

    [EnvParameter("REDIS_PORT")]
    [ConfigParameter("redis", "port", Required = true)]
    public int RedisPort { get; set; } = 6379;

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
