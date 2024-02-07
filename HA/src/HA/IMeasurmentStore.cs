namespace HA;

public interface IMeasurmentStore
{
    void WriteMeasurement(Measurement measurement);

    void WriteMeasurements(IEnumerable<Measurement> measurements);
}