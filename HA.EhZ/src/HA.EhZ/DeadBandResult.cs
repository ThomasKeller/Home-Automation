using System;

namespace HA.EhZ;

public class DeadBandResult
{
    public DateTime TimeStamp { get; set; }

    public double Value { get; set; }

    public double Difference { get; set; }

    public TimeSpan TimeDifference { get; set; }

    public int ValueCompressed { get; set; }

    public double Power => CalculatePower();

    private double CalculatePower()
    {
        if (TimeDifference.TotalHours > 0.0)
        {
            return Math.Round(Difference * 1000.0 / TimeDifference.TotalHours, 2);
        }
        return 0.0;
    }
}