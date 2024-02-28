namespace HA;

public class ValueWithStatistic<T> 
{
    private T? _value;
    private DateTime _dateCountStart;
    private int _durationCount = 0;
    private List<int> _durationCounts = new();

    public ValueWithStatistic(T? startValue = default)
    {
        _dateCountStart = DateTime.Now;
        SetValue(startValue);
    }

    public List<int> DurationCounts { get { return new List<int>(_durationCounts); } }

    public int DurationCount { get { return _durationCount; } }

    public int LastDurationCount { get; set; }

    public TimeSpan CountTimeSpan => DateTime.Now - _dateCountStart;

    public DateTime LastChangeTime { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(15);

    public T? Value { 
        get { return _value ; }
        set { SetValue(value); }
    }

    public T? LastValue { get; private set; }

    private void SetValue(T? newValue)
    {
        LastValue = _value;
        _value = newValue;
        LastChangeTime = DateTime.Now;
        if (DateTime.Now > _dateCountStart + Duration)
        {
            _dateCountStart = DateTime.Now;
            // reset count
            LastDurationCount = _durationCount + 1;
            _durationCounts.Add(LastDurationCount);
            if (_durationCounts.Count > 10) 
                _durationCounts.RemoveAt(0);
            _durationCount = 0;
        }
        else
            _durationCount++;
    }

    public override string ToString()
    {
        return $"Value: {_value} LastValue: {LastValue} Changed: {LastChangeTime} ChangeCount: {DurationCount} / {Duration.TotalSeconds} s";
    }

    public string ToShortString()
    {
        return $"{_value} | {LastChangeTime} | ChangeCount: {DurationCount} / {Duration.TotalSeconds} s";
    }

}
