namespace HA.Influx;

public interface IInfluxStore
{
    bool CheckHealth();

    bool Ping();

    void WriteMeasurement(Measurement measurement);

    void WriteMeasurements(IEnumerable<Measurement> measurements);
}