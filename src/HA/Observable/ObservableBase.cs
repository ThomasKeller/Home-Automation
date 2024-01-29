namespace HA.Observable;

/// <summary>
/// Provides a common pattern to automatically unsubscribe
/// diposed observer.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObservableBase<T> : IObservable<T>
{
    protected readonly List<IObserver<T>> _observers = new();

    public DateTime LastOnNext { get; private set; } = DateTime.MinValue;

    public DateTime LastOnError { get; private set; } = DateTime.MinValue;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
        return new Unsubscriber<T>(_observers, observer);
    }

    public void ExecuteOnNext(T value)
    {
        LastOnNext = DateTime.Now;
        foreach (var observer in _observers)
        {
            observer?.OnNext(value);
        }
    }

    public void ExecuteOnComplete()
    {
        LastOnError = DateTime.Now;
        foreach (var observer in _observers)
        {
            observer?.OnCompleted();
        }
        _observers.Clear();
    }

    public void ExecuteOnError(Exception error)
    {
        foreach (var observer in _observers)
        {
            observer?.OnError(error);
        }
    }
}