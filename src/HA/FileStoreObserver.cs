using Microsoft.Extensions.Logging;

namespace HA;

public class FileStoreObserver : IObserver<Measurement>
{
    private readonly ILogger _logger;
    private readonly IFileStore _fileStore;
    private IDisposable? _unsubscriber;

    public FileStoreObserver(ILogger logger, IFileStore fileStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
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
        _logger.LogInformation("FileStoreObserver:OnCompleted");
    }

    public virtual void OnError(Exception error)
    {
        _logger.LogError(error, "FileStoreObserver:OnError");
    }

    public virtual void OnNext(Measurement value)
    {
        _fileStore.WriteToFile(value.ToLineProtocol());
    }
}