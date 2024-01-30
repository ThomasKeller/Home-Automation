using System.Collections.Generic;

namespace HA.IOBroker.config;

public class DeviceGroupConfig
{
    public string GroupName { get; set; }

    public Dictionary<string, PropertyDetail> Properties { get; set; }
}