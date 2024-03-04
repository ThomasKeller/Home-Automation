namespace HA;

[Flags]
public enum MeasurementOptions
{
    None = 0,
    StoreToTimeSeriesDb = 1,
    StoreToDisk = 2,
    StoreToDb = 4,
    PublishToMqtt = 8,
    StoreAndPublish = StoreToTimeSeriesDb | PublishToMqtt,
}
