namespace HA.Store;

public static class StoreExtensions
{
    public static MeasurementEntity ToEntity(this Measurement measurement)
    {
        if (measurement == null)
            throw new ArgumentNullException(nameof(measurement));
        return new MeasurementEntity
        {
            CreatedOn = measurement.GetUtcTimeStamp(),
            LineProtocol = measurement.ToLineProtocol()
        };
    }

    public static Measurement ToMeasurement(this MeasurementEntity entity)
    {
        if (entity == null || string.IsNullOrEmpty(entity.LineProtocol))
            throw new ArgumentNullException(nameof(entity));
        var measurement = Measurement.FromLineProtocol(entity.LineProtocol);
        if (measurement == null)
            throw new ArgumentNullException("measuremnent");
        measurement.ExternalId = entity.Id.ToString();
        return measurement;
    }
}