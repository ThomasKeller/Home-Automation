using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace HA.Service.Settings;

public class MqttSettings : AppSettingsBase
{
    public MqttSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }

    [EnvParameter("MQTT_HOST")]
    [ConfigParameter("mqtt", "host", Required = true)]
    public string MqttHost { get; set; } = "localhost";

    [EnvParameter("MQTT_PORT")]
    [ConfigParameter("mqtt", "port", Required = true)]
    public int MqttPort { get; set; } = 1883;

    [EnvParameter("MQTT_CLIENTID")]
    [ConfigParameter("mqtt", "clientID", Required = true)]
    public string MqttClientId { get; set; } = CreateUniqueClientName();


    private static string CreateUniqueClientName()
    {
        var machineName = Environment.MachineName;
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        return $"{machineName}-{assemblyName}";
    }

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
