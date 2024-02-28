using Microsoft.Extensions.Configuration;

namespace HA.Service.Settings;

public class NatsSettings : AppSettingsBase
{
    #pragma warning disable CS8618 
    public NatsSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }
    #pragma warning restore CS8618 

    [EnvParameter("NATS_URL")]
    [ConfigParameter("nats", "url", Required = true)]
    public string Url { get; set; } = "nats://192.168.111.49:4222/";

    [EnvParameter("NATS_CLIENTNAME")]
    [ConfigParameter("nats", "clientName", Required = false)]
    public string ClientName { get; set; }

    [EnvParameter("NATS_USER")]
    [ConfigParameter("nats", "user", Required = true)]
    public string User { get; set; }

    [EnvParameter("NATS_PASSWORD")]
    [ConfigParameter("nats", "password", Required = true)]
    public string Password { get; set; }

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
