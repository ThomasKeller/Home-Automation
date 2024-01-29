using System.Globalization;

namespace HA;

public class ValueConverter
{
    public ValueConverter()
    {
        UsedCultureInfo = CultureInfo.InvariantCulture;
        UsedNumberStyle = NumberStyles.Any;
    }

    public CultureInfo UsedCultureInfo { get; set; }

    public NumberStyles UsedNumberStyle { get; set; }

    public static T? ConvertOrDefault<T>(IReadOnlyDictionary<string, object> values, string key, T? defaultValue = default)
    {
        try
        {
            if (values.ContainsKey(key))
            {
                object obj = values[key];
                if (obj != null) return (T)obj;
            }
        }
        catch
        {
        }
        return defaultValue;
    }

    public Dictionary<string, object> Convert(IReadOnlyDictionary<string, string> parsedValues, IReadOnlyDictionary<string, TypeCode> valueTypes)
    {
        var result = new Dictionary<string, object>();
        if (parsedValues == null)
            return result;
        foreach (var key in parsedValues.Keys)
        {
            if (valueTypes.ContainsKey(key))
            {
                var convertedValue = ConvertTo(parsedValues[key], valueTypes[key]);
                if (convertedValue != null)
                {
                    result.Add(key, convertedValue);
                }
                continue;
            }
            object obj = parsedValues[key];
            if (obj == null) continue;
            result.Add(key, obj);
        }
        return result;
    }

    public T? ConvertOrDefault<T>(object? value, T? defaultValue = default)
    {
        try
        {
            if (value != null) return (T)value;
        }
        catch
        {
        }
        return defaultValue;
    }

    public object? ConvertTo(string valueAsText, TypeCode targetType)
    {
        object? convertedValue = null;
        bool convertState = false;
        switch (targetType)
        {
            case TypeCode.Boolean:
                bool boolvalue;
                convertState = bool.TryParse(valueAsText, out boolvalue);
                convertedValue = boolvalue;
                break;

            case TypeCode.Int16:
                short i16value;
                convertState = short.TryParse(valueAsText, out i16value);
                convertedValue = i16value;
                break;

            case TypeCode.Int32:
                int i32value;
                convertState = int.TryParse(valueAsText, out i32value);
                convertedValue = i32value;
                break;

            case TypeCode.Int64:
                long i64value;
                convertState = long.TryParse(valueAsText, UsedNumberStyle, UsedCultureInfo, out i64value);
                convertedValue = i64value;
                break;

            case TypeCode.Single:
                float fvalue;
                convertState = float.TryParse(valueAsText, UsedNumberStyle, UsedCultureInfo, out fvalue);
                convertedValue = fvalue;
                break;

            case TypeCode.Double:
                double dvalue;
                convertState = double.TryParse(valueAsText, UsedNumberStyle, UsedCultureInfo, out dvalue);
                convertedValue = dvalue;
                break;

            case TypeCode.String:
                convertState = string.IsNullOrEmpty(valueAsText) == false;
                convertedValue = valueAsText;
                break;

            default:
                throw new Exception("type currently not supported: " + targetType.ToString());
        }
        return convertState ? convertedValue : null;
    }

    public double ToDoubleOrDefault(string valueAsString, double defaultValue = 0.0)
    {
        return ConvertOrDefault(
            ConvertTo(valueAsString, TypeCode.Double),
            defaultValue);
    }

    public int ToIntOrDefault(string valueAsString, int defaultValue = 0)
    {
        return ConvertOrDefault(
            ConvertTo(valueAsString, TypeCode.Int32),
            defaultValue);
    }
}