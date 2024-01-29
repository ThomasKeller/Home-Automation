using System;

namespace HA.EhZ;

public sealed class EhZMeasurement
{
    public string SourceName => "EhZ_Energy";

    public DateTime MeasuredUtcTime { get; set; }

    public double ConsumedEnergy1 { get; set; } // Wh

    public double ProducedEnergy1 { get; set; } // Wh

    public double ConsumedEnergy2 { get; set; } // Wh

    public double ProducedEnergy2 { get; set; } // Wh

    public double CurrentPower { get; set; } // W
}