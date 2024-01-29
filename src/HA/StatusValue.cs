namespace HA;

public class StatusValue<T>
{
    private T _value;

    public StatusValue(T init)
    {
        _value = init;
    }

    public T Value
    { get { return _value; } set { ChangedOn = DateTime.Now; _value = value; } }

    public DateTime ChangedOn { get; private set; } = DateTime.MinValue;
}