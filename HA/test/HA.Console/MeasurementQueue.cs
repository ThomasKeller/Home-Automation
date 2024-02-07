using System.Collections.Concurrent;

namespace HA.ConsoleApp;

public class MeasurementQueue
{
    private readonly ConcurrentQueue<Measurement> _measurementQueue = new ();

    public int Count => _measurementQueue.Count;

    public void AddMeasurement(Measurement measurement)
    {
        _measurementQueue.Enqueue(measurement);
    }

    public Measurement? Dequeue()
    {
        if (_measurementQueue.TryDequeue(out var measurement)) 
            return measurement;
        return null;
    }

    public Measurement? Peek()
    {
        if (_measurementQueue.TryPeek(out var measurement))
            return measurement;
        return null;
    }
}
