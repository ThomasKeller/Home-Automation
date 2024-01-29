using System;

namespace HA.EhZ.Tests;

public class ObserverTest<T> : IObserver<T>
{
    public void OnCompleted()
    {
        Console.WriteLine("OnComplete");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine(error.Message);
    }

    public void OnNext(T value)
    {
        Console.WriteLine("OnNext");
    }
}