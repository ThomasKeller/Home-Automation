using System;

namespace HA.EhZ;

public class DeadBand
{
    private readonly object m_LockObject = new object();

    public DeadBandResult FirstValue { get; private set; }

    public int Count { get; private set; }

    public TimeSpan TimeDeadBand { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan ValuesEqualDeadBand { get; set; } = TimeSpan.FromMinutes(10);

    public DeadBandResult AddValue(DateTime measuredTime, double value)
    {
        lock (m_LockObject)
        {
            if (FirstValue == null)
            {
                FirstValue = new DeadBandResult { TimeStamp = measuredTime, Value = value };
                Count++;
                return null;
            }
            if (measuredTime - FirstValue.TimeStamp >= TimeDeadBand)
            {
                var result = new DeadBandResult
                {
                    TimeStamp = measuredTime,
                    Value = value,
                    Difference = value - FirstValue.Value,
                    TimeDifference = measuredTime - FirstValue.TimeStamp,
                    ValueCompressed = Count + 1
                };
                if (result.Difference == 0 && result.TimeDifference < ValuesEqualDeadBand)
                {
                    // value didn't change and will be reported afer NoDifferenceDeadBand is reached
                    Count++;
                    return null;
                }
                FirstValue = new DeadBandResult { TimeStamp = measuredTime, Value = value };
                Count = 0;
                return result;
            }
            else
            {
                Count++;
            }
        }
        return null;
    }
}