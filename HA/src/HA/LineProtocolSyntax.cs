using System.Globalization;

namespace HA;

public static class LineProtocolSyntax
{
    public static readonly DateTime Origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Dictionary<Type, Func<object, string>> Formatters = new Dictionary<Type, Func<object, string>>
    {
        { typeof(sbyte), FormatInteger },
        { typeof(byte), FormatInteger },
        { typeof(short), FormatInteger },
        { typeof(ushort), FormatInteger },
        { typeof(int), FormatInteger },
        { typeof(uint), FormatInteger },
        { typeof(long), FormatInteger },
        { typeof(ulong), FormatInteger },
        { typeof(float), FormatFloat },
        { typeof(double), FormatFloat },
        { typeof(decimal), FormatFloat },
        { typeof(bool), FormatBoolean },
        { typeof(TimeSpan), FormatTimespan }
    };

    public static string EscapeName(string? nameOrKey)
    {
        if (nameOrKey == null) throw new ArgumentNullException(nameof(nameOrKey));
        return nameOrKey
            .Replace("=", "\\=")
            .Replace(" ", "\\ ")
            .Replace(",", "\\,");
    }

    public static string UnescapeName(string? nameOrKey)
    {
        if (nameOrKey == null) throw new ArgumentNullException(nameof(nameOrKey));
        return nameOrKey
            .Replace("\\=", "=")
            .Replace("\\ ", " ")
            .Replace("\\,", ",");
    }

    public static string FormatValue(object value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (Formatters.TryGetValue(value.GetType(), out var format))
            return format(value);
        return FormatString($"{value}");
    }

    private static string FormatInteger(object i)
    {
        return ((IFormattable)i).ToString(null, CultureInfo.InvariantCulture) + "i";
    }

    private static string FormatFloat(object f)
    {
        return ((IFormattable)f).ToString(null, CultureInfo.InvariantCulture);
    }

    private static string FormatTimespan(object ts)
    {
        return ((TimeSpan)ts).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatBoolean(object b)
    {
        return (bool)b ? "t" : "f";
    }

    private static string FormatString(string s)
    {
        return "\"" + s.Replace("\"", "\\\"") + "\"";
    }

    public static string FormatTimestamp(DateTime utcTimestamp, TimeResolution resolution)// = TimeResolution.ns)
    {
        return utcTimestamp.ToEpochString(resolution);
        //var t = utcTimestamp - Origin;
        //return (t.Ticks * 100L).ToString(CultureInfo.InvariantCulture);
    }
}