namespace HA;

public class MeasurementException : Exception
{
    public Measurement? Measurement { get; protected set; }

    public MeasurementException(Measurement measurement)
    {
        Measurement = measurement;
    }

    public MeasurementException(string message, Measurement measurement)
        : base(message)
    {
        Measurement = measurement;
    }

    public MeasurementException(string message, Exception innerException, Measurement measurement)
        : base(message, innerException)
    {
        Measurement = measurement;
    }
}