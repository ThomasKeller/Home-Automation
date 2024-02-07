﻿using Microsoft.Extensions.Logging;

namespace HA;

public class MeasurementObserver : IObserver<Measurement>
{
    private readonly ILogger _logger;
    private readonly IObserverProcessor _processor;
    private IDisposable? _unsubscriber;

    public MeasurementObserver(ILogger logger, IObserverProcessor processor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    public DateTime LastMeasurementProccessed { get; private set; } = DateTime.MinValue;

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
        _logger.LogInformation(AddThreadIDPrefix("OnCompleted"));
    }

    public virtual void OnError(Exception error)
    {
        _logger.LogError(error, AddThreadIDPrefix("OnError"));
    }

    public virtual void OnNext(Measurement value)
    {
        LastMeasurementProccessed = DateTime.Now;
        _logger.LogDebug(AddThreadIDPrefix($"OnNext {DateTime.Now.ToShortTimeString()}"));
        _processor.ProcessMeasurement(value);
    }

    private string AddThreadIDPrefix(string message)
    {
        return $"[TID:{Thread.CurrentThread.ManagedThreadId}] {message}";
    }
}