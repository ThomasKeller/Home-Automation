using Microsoft.Extensions.Logging;

namespace HA;

public class MeasurementActor : IObserver<Measurement>
{
    private readonly ILogger _logger;
    private readonly Action<Measurement> _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action? _onComplete;

    private IDisposable? _unsubscriber;

    public MeasurementActor(ILogger logger, Action<Measurement> onNext, Action<Exception>? onError = null,  Action? onComplete = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onError = onError;
        _onComplete = onComplete;
    }

    public DateTime LastMeasurementProccessed { get; private set; } = DateTime.MinValue;

    public void Subscribe(IObservable<Measurement> provider)
    {
        _unsubscriber = provider.Subscribe(this);
    }

    public void Unsubscribe()
    {
        _unsubscriber?.Dispose();
    }

    public void OnCompleted()
    {
        _logger.LogInformation(AddThreadIDPrefix("OnCompleted"));
        _onComplete?.Invoke();
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, AddThreadIDPrefix("OnError"));
        _onError?.Invoke(error);
    }

    public void OnNext(Measurement value)
    {
        LastMeasurementProccessed = DateTime.Now;
        _logger.LogDebug(AddThreadIDPrefix($"OnNext {DateTime.Now.ToShortTimeString()}"));
        _onNext.Invoke(value);
    }

    private string AddThreadIDPrefix(string message)
    {
        return $"[TID:{Thread.CurrentThread.ManagedThreadId}] {message}";
    }
}