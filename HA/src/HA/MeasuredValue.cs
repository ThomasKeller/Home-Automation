namespace HA;

public class MeasuredValue
{
    public string? Name { get; set; }

    public object? Value { get; set; }

    public static MeasuredValue Create(string name, object Value)
    {
        return new MeasuredValue { Name = name, Value = Value };
    }
}