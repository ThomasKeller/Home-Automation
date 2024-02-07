namespace HA;

public class ConsoleObserver : IObserver<Measurement>
{
    private IDisposable? _unsubscriber;

    public ConsoleObserver()
    {
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
        Console.WriteLine("OnCompleted");
    }

    public virtual void OnError(Exception error)
    {
        Console.WriteLine("Exception: {0}", error.Message);
    }

    public virtual void OnNext(Measurement value)
    {
        Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} Measurement: {value.GetTimeStamp().ToString("s")} {value.Device} {value.ToLineProtocol()}");
    }
}