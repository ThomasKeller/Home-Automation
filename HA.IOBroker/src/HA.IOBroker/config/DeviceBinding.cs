using System.Collections.Generic;

namespace HA.IOBroker.config;

public class DeviceBinding
{
    public string GroupName { get; set; }

    public string DeviceName { get; set; }

    public string DeviceId { get; set; }

    public Dictionary<string, PropertyDetail> Properties { get; set; }

    public string[] GetDeviceIds()
    {
        var result = new List<string>();
        foreach (var propertyKey in Properties.Keys)
        {
            result.Add($".{DeviceId}.{propertyKey}");
        }
        return result.ToArray();
    }
}