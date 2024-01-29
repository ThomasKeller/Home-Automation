using Microsoft.Extensions.Logging;

namespace HA.Influx;

public class InfluxResilientObserver : IObserver<Measurement>
{
    private readonly ILogger _logger;
    private readonly IInfluxStore _influxStore;
    private IDisposable? _unsubscriber;

    public InfluxResilientObserver(ILogger logger, IInfluxStore influxStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _influxStore = influxStore ?? throw new ArgumentNullException(nameof(influxStore));
    }

    public virtual void Subscribe(IObservable<Measurement> provider)
    {
        _unsubscriber = provider.Subscribe(this);
    }

    public virtual void Unsubscribe()
    {
        _unsubscriber?.Dispose();
    }

    public virtual void OnCompleted()
    {
        _logger.LogInformation("OnCompleted");
    }

    public virtual void OnError(Exception error)
    {
        _logger.LogError(error, "OnError");
    }

    public virtual void OnNext(Measurement value)
    {
        _influxStore.WriteMeasurement(value);
    }
}