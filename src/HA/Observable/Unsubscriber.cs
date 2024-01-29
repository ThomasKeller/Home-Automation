namespace HA.Observable;

/// <summary>
/// It is used to automatically unsubcribe from the Observable
/// when the Observer dispose
/// </summary>
/// <typeparam name="T">type of unsubscriber</typeparam>
public class Unsubscriber<T> : IDisposable
{
    private readonly IObserver<T> _observer;
    private readonly List<IObserver<T>> _observers;

    public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
    {
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    public void Dispose()
    {
        if (_observer != null)
        {
            _observers.Remove(_observer);
        }
    }
}