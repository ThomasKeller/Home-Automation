namespace HA;

/// <summary>
/// A class that provides sun informations of a given day on a given location
/// represented by latitude and longtitude.
/// </summary>
public static class Sun
{
    public class SunValues
    {
        /// <summary>
        /// DateTime representation of the sunrise-timestamp of a given day on a given location.
        /// </summary>
        public DateTime Sunrise { get; set; }

        /// <summary>
        /// DateTime representation of the sunset-timestamp of a given day on a given location.
        /// </summary>
        public DateTime Sunset { get; set; }
    }

    public static SunValues CalculatePvTime()
    {
        var today = DateTime.Today;
        var sunValues = Calculate(51.194256, 6.400471, today.Year, today.Month, today.Day);
        sunValues.Sunrise = sunValues.Sunrise.AddMinutes(-60);
        sunValues.Sunset = sunValues.Sunset.AddMinutes(60);
        return sunValues;
    }

    public static SunValues CalculatePvTime(double latitude = 51.194256, double longtitude = 6.400471)
    {
        var today = DateTime.Today;
        var sunValues = Calculate(latitude, longtitude, today.Year, today.Month, today.Day);
        sunValues.Sunrise = sunValues.Sunrise.AddMinutes(-60);
        sunValues.Sunset = sunValues.Sunset.AddMinutes(60);
        return sunValues;
    }

    public static SunValues Calculate(double latitude = 51.194256, double longtitude = 6.400471)
    {
        var today = DateTime.Today;
        return Calculate(latitude, longtitude, today.Year, today.Month, today.Day);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sun"/> class.
    /// </summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longtitude">The longtitude.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    public static SunValues Calculate(double latitude, double longtitude, int year, int month, int day)
    {
        var pi = Math.PI;
        var dr = pi / 180;
        var rd = 1 / dr;
        var b5 = latitude;
        var l5 = longtitude;
        var h = 0;    // timezone UTC
        var now = DateTime.Now;
        var m = month;
        var d = day;
        b5 = dr * b5;
        var n = 275 * m / 9 - 2 * ((m + 9) / 12) + d - 30;
        var l0 = 4.8771 + .0172 * (n + .5 - l5 / 360);
        var c = .03342 * Math.Sin(l0 + 1.345);
        var c2 = rd * (Math.Atan(Math.Tan(l0 + c)) - Math.Atan(.9175 * Math.Tan(l0 + c)) - c);
        var sd = .3978 * Math.Sin(l0 + c);
        var cd = Math.Sqrt(1 - sd * sd);
        var sc = (sd * Math.Sin(b5) + .0145) / (Math.Cos(b5) * cd);
        var sunrise = DateTime.MinValue;
        var sunset = DateTime.MinValue;

        if (Math.Abs(sc) <= 1)
        {
            // calculate sunrise
            var c3 = rd * Math.Atan(sc / Math.Sqrt(1 - sc * sc));
            var r1 = 6 - h - (l5 + c2 + c3) / 15;
            var hr = (int)r1;
            var mr = (int)((r1 - hr) * 60);
            sunrise = new DateTime(year, month, day, hr, mr, 0, DateTimeKind.Utc);
            // calculate sunset
            var s1 = 18 - h - (l5 + c2 - c3) / 15;
            var hs = (int)s1;
            var ms = (int)((s1 - hs) * 60);
            sunset = new DateTime(year, month, day, hs, ms, 0, DateTimeKind.Utc);
        }
        else
        {
            if (sc > 1)
            {
                // sun is up all day ...
                // Set Sunset to be in the future ...
                sunset = new DateTime(now.Year + 1, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
                // Set Sunrise to be in the past ...
                sunrise = new DateTime(now.Year - 1, now.Month, now.Day, now.Hour, now.Minute - 1, now.Second, DateTimeKind.Utc);
            }
            if (sc < -1)
            {
                // sun is down all day ...
                // Set Sunrise and Sunset to be in the future ...
                sunrise = new DateTime(now.Year + 1, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
                sunset = new DateTime(now.Year + 1, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
            }
        }
        return new SunValues { Sunrise = sunrise.ToLocalTime(), Sunset = sunset.ToLocalTime() };
    }
}