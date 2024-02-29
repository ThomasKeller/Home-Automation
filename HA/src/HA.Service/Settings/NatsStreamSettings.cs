using Microsoft.Extensions.Configuration;

namespace HA.Service.Settings;

public class NatsStreamSettings : AppSettingsBase
{
    #pragma warning disable CS8618 
    public NatsStreamSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }
    #pragma warning restore CS8618 

    [EnvParameter("NATS_STREAMNAME")]
    [ConfigParameter("nats", "stream_name", Required = false)]
    public string StreamName { get; set; } // MEASUREMENTS

    [EnvParameter("NATS_SUBJECT")]
    [ConfigParameter("nats", "subject", Required = false)]
    public string Subject { get; set; }  // measurments.>

    [EnvParameter("NATS_SUBJECT_PREFIX")]
    [ConfigParameter("nats", "subjectPrefix", Required = false)]
    public string SubjectPrefix { get; set; }  // measurments.new

    [EnvParameter("NATS_MAX_AGE_DAYS")]
    [ConfigParameter("nats", "maxAgeDays", Required = false)]
    public int maxAgeInDays { get; set; } = 14;

    public void CheckRequiredProperties()
    {
        CheckSettings();
    }
}
