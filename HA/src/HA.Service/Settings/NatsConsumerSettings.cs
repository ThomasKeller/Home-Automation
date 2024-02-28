using Microsoft.Extensions.Configuration;

namespace HA.Service.Settings;

public class NatsConsumerSettings : AppSettingsBase
{
    #pragma warning disable CS8618 
    public NatsConsumerSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }
    #pragma warning restore CS8618 

    [EnvParameter("NATS_CONSUMERNAME")]
    [ConfigParameter("nats", "consumer_name", Required = false)]
    public string ConsumerName { get; set; } // Consumer1

    [EnvParameter("NATS_STREAMNAME")]
    [ConfigParameter("nats", "stream_name", Required = false)]
    public string StreamName { get; set; } // MEASUREMENTS

    [EnvParameter("NATS_FILTERED_SUBJECT")]
    [ConfigParameter("nats", "filteredSubject", Required = false)]
    public string FilteredSubject { get; set; }  // measurments.>

    [EnvParameter("NATS_QUEUE_GROUP")]
    [ConfigParameter("nats", "queueGroup", Required = false)]
    public string QueueGroup { get; set; }

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
