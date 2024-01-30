using System;

namespace HA.IOBroker;

public class IoBrokerValue : ICloneable
{
    public string GroupName { get; set; }

    public string Channel { get; set; }

    public string DeviceId { get; set; }

    public string DeviceName { get; set; }

    public string PropertyName { get; set; }

    public DateTime TimeStamp { get; set; }

    public object Value { get; set; }

    public override string ToString()
    {
        return $"{TimeStamp}: {Channel} | {Value}";
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}