using HA.Observable;
using Microsoft.Extensions.Logging;
using System;

namespace HA.EhZ.Observable;

/// <summary>
/// Parser observable and observer.
/// The observer could subscribe the SerialPortObservable to read the byte stream from
/// the serial port. After parsing the byte stream the OnNeext method is called with
/// measurement result.
/// </summary>
public sealed class ParserObservable : ObservableBase<Measurement>, IObserver<byte[]>
{
    private readonly ILogger _logger;
    private const string c_EhZEnergy = "EhZ_Energy";
    private readonly DeadBand m_ConsumedEnergyDeadBand = new DeadBand();
    private readonly DeadBand m_ProducedEnergyDeadBand = new DeadBand();
    private readonly SmlParser m_SmlParser = new SmlParser();

    public DateTime LastMeasurementSentAt { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="timeDeadBand">ignore all new values during this interval</param>
    /// <param name="valuesEqualDeadBand">call OnNext methods after this time even the value didn't change</param>
    public ParserObservable(ILogger logger, TimeSpan timeDeadBand, TimeSpan valuesEqualDeadBand)
    {
        _logger = logger;
        m_ConsumedEnergyDeadBand = new DeadBand { TimeDeadBand = timeDeadBand, ValuesEqualDeadBand = valuesEqualDeadBand };
        m_ProducedEnergyDeadBand = new DeadBand { TimeDeadBand = timeDeadBand, ValuesEqualDeadBand = valuesEqualDeadBand };
    }

    public void OnCompleted()
    {
        _logger.LogInformation("ParserObservable::OnComplete");
        OnCompleted();
    }

    public void OnError(Exception error)
    {
        _logger.LogError("ParserObservable::OnError {0}", error.Message);
        OnError(error);
    }

    public void OnNext(byte[] value)
    {
        var ehZMeasurement = m_SmlParser.AddBytes(value);
        if (ehZMeasurement != null)
        {
            var measurement = new Measurement
            {
                Device = "EhZ",
                Quality = QualityInfos.Good,
                Ticks = ehZMeasurement.MeasuredUtcTime.ToLocalTime().Ticks,
            };
            measurement.Values.Add(MeasuredValue.Create("ConsumedEnergy", ehZMeasurement.ConsumedEnergy1));
            measurement.Values.Add(MeasuredValue.Create("ConsumedEnergy2", ehZMeasurement.ConsumedEnergy2));
            measurement.Values.Add(MeasuredValue.Create("ProducedEnergy", ehZMeasurement.ProducedEnergy1));
            measurement.Values.Add(MeasuredValue.Create("ProducedEnergy2", ehZMeasurement.ProducedEnergy2));
            measurement.Values.Add(MeasuredValue.Create("PowerConsumption", ehZMeasurement.CurrentPower));
            if (measurement.Values.Count > 0)
            {
                ExecuteOnNext(measurement);
            }
        }
    }
}