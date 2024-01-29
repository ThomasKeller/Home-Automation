namespace HA;

public interface IObserverProcessor
{
    void ProcessMeasurement(Measurement measurement);
}