namespace HA.Influx;

public interface IInfluxStore : IMeasurmentStore
{
    bool CheckHealth();

    bool Ping();
}