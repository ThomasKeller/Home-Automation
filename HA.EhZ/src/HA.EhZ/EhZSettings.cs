using HA.AppTools;
using Microsoft.Extensions.Configuration;

namespace HA.EhZ;

public class EhZSettings : AppSettingsBase
{
    public EhZSettings(IConfiguration? configuration)
    {
        if (configuration != null)
        {
            ReadAppConfigFile(configuration);
        }
        ReadEnvironmentVariables();
    }

    [EnvParameter("USE_SERIALPORT")]
    [ConfigParameter("ehz", "useSerialPort", Required = false)]
    public bool UseSerialPort { get; set; } = true;

    [EnvParameter("SERIALPORT")]
    [ConfigParameter("ehz", "serialPort", Required = false)]
    public string SerialPort { get; set; }

    [EnvParameter("UDP_PORT_IN")]
    [ConfigParameter("ehz", "udp_port_in", Required = false)]
    public int UdpPortIn { get; set; } = 5557;

    [EnvParameter("ENABLE_UDP_SERVER")]
    [ConfigParameter("ehz", "enableUdpServer", Required = false)]
    public bool EnableUdpServer { get; set; } = false;
        
    [EnvParameter("UDP_PORT_OUT")]
    [ConfigParameter("ehz", "udp_port_out", Required = false)]
    public int UdpPortOut { get; set; } = 5558;

    [EnvParameter("LINEPROTOCOL_LOG_PATH")]
    [ConfigParameter("ehz", "lineprotocol_log_path", Required = false)]
    public string LineprotocolLogPath { get; set; }
}
