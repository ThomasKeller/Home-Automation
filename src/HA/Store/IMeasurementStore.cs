namespace HA.Store;

public interface IMeasurementStore
{
    int Count();

    int Save(Measurement measurement);

    int Save(IEnumerable<Measurement> measurements);

    int Remove(int id);

    int Remove(IEnumerable<int> ids);

    IEnumerable<MeasurementEntity> GetAll();
}