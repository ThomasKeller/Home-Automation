using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HA;

public static class EpochExtensions
{
    private static readonly DateTime s_Origin = new DateTime(new DateTime(1970, 1, 1).Ticks, DateTimeKind.Utc);

    /// <summary>
    /// Convert date time value into a epoch long value
    /// https://www.epochconverter.com/
    /// </summary>
    /// <param name="time">date time value</param>
    /// <param name="resolution">defines how to interpret the long value</param>
    /// <returns></returns>
    public static long ToEpoch(this DateTime time, TimeResolution resolution = TimeResolution.ns)
    {
        // max time resolution in datetime:
        // 1 tick = 100 ns
        // ns * 100
        // µs / 10
        // ms / 10000
        // s /  10000000
        var timediff = time - s_Origin;
        var timediffTicks = timediff.Ticks;
        switch (resolution)
        {
            case TimeResolution.s: return (timediffTicks / 10000000);
            case TimeResolution.ms: return (timediffTicks / 10000);
            case TimeResolution.us: return (timediffTicks / 10);
            case TimeResolution.ns: return (timediffTicks * 100);
            default: throw new NotImplementedException($"Implementation missing for {resolution}");
        }
    }

    public static DateTime FromEpoch(this long duration, TimeResolution resolution = TimeResolution.ns)
    {
        // max time resolution in datetime:
        // 1 tick = 100 ns
        // ns * 100
        // µs / 10
        // ms / 10000
        // s /  10000000
        long timeDiffTicks = 0;
        switch (resolution)
        {
            case TimeResolution.s: timeDiffTicks = duration * 10000000; break;
            case TimeResolution.ms: timeDiffTicks = duration * 10000; break;
            case TimeResolution.us: timeDiffTicks = duration * 10; break;
            case TimeResolution.ns: timeDiffTicks = duration / 100; break;;
            default: throw new NotImplementedException($"Implementation missing for {resolution}");
        }
        return s_Origin.AddTicks(timeDiffTicks); //1 tick = 100 nano sec
    }

    public static string ToEpochString(this DateTime time, TimeResolution resolution = TimeResolution.ns)
    {
        // max time resolution in datetime:
        // 1 tick = 100 ns
        // ns * 100
        // µs / 10
        // ms / 10000
        // s /  10000000
        var timediff = time - s_Origin;
        var timediffTicks = timediff.Ticks;
        var result = $"{timediffTicks}0000000000000000000";
        switch (resolution)
        {
            // 1669473029372994100 ns
            // 1669473029372994    µs
            // 1669473029372       ms
            // 1669473029          s
            case TimeResolution.s: return result.Substring(0, 10);
            case TimeResolution.ms: return result.Substring(0, 13);
            case TimeResolution.us: return result.Substring(0, 16);
            case TimeResolution.ns: return result.Substring(0, 19);
            default: throw new NotImplementedException($"Implementation missing for {resolution}");
        }
    }
}