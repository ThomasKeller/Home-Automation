using Microsoft.Extensions.Logging;

namespace HA.Redis;

public class RedisPushObserver : IObserver<Measurement>
{
    private readonly ILogger _logger;
    private readonly IRedisPushToStreamClient _pushToStreamClient;
    private IDisposable? _unsubscriber;

    public RedisPushObserver(ILogger logger, IRedisPushToStreamClient pushToStreamClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pushToStreamClient = pushToStreamClient ?? throw new ArgumentNullException(nameof(pushToStreamClient));
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
        _pushToStreamClient.PushToStream(value);
    }
}