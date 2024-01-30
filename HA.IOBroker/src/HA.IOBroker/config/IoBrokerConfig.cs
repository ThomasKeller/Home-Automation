using System.Collections.Generic;
using System.Linq;

namespace HA.IOBroker.config;

public class IoBrokerConfig
{
    public List<DeviceGroupConfig> DeviceGroups { get; set; }

    public List<DeviceConfig> Devices { get; set; }

    public IEnumerable<DeviceBinding> BuildBindings()
    {
        foreach (var device in Devices)
        {
            var group = DeviceGroups.FirstOrDefault(dg => dg.GroupName == device.GroupName);
            yield return new DeviceBinding
            {
                DeviceName = device.DeviceName,
                DeviceId = device.DeviceId,
                GroupName = device.GroupName,
                Properties = group.Properties,
            };
        }
    }
}