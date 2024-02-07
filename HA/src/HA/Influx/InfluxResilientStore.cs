using HA.Store;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Timers;

namespace HA.Influx;

public partial class InfluxResilientStore : IInfluxStore, IObserverProcessor
{
    public class Status
    {
        public StatusValue<bool> IsHealthy { get; set; } = new StatusValue<bool>(false);

        public StatusValue<bool> IsConnected { get; set; } = new StatusValue<bool>(false);

        public StatusValue<bool> IsPingSuccessful { get; set; } = new StatusValue<bool>(false);
    }

    private static readonly TaskFactory _taskFactory = new(
        CancellationToken.None,
        TaskCreationOptions.None,
        TaskContinuationOptions.None,
        TaskScheduler.Default);

    private readonly ILogger _logger;
    private readonly IInfluxStore _influxSimpleStore;
    private readonly IMeasurementStore _store;
    private readonly ConcurrentQueue<Measurement> _measurements = new();
    private readonly System.Timers.Timer _connectionTimer;
    private readonly object _lock = new();
    private DateTime _taskStartTime = DateTime.MinValue;
    private int _taskCount = 0;
    private int _maxTaskCount = 2;
    private bool _UnauthorizedStatus = false;
    private bool _influxConnected = true;
    private bool _recoveryInProgress = false;

    public Status StoreStatus { get; private set; } = new Status();

    public int QueueCount => _measurements.Count;

    public int InfluxErrorCount { get; private set; } = 0;

    public int WriteAfterMs { get; set; } = 2000;

    public int BatchSize { get; set; } = 50;

    public bool InfluxHealthy { get; set; } = false;

    public IDictionary<string, object> GetStatus()
    {
        var prefix = "InfluxResilientStore-";
        var status = new Dictionary<string, object>
        {
            { $"{prefix}Connected", _influxConnected },
            { $"{prefix}InfluxHealthy", InfluxHealthy },
            { $"{prefix}RecoveryInProgress", _recoveryInProgress },
            { $"{prefix}QueueCount", QueueCount },
            { $"{prefix}InfluxErrorCount", InfluxErrorCount },
            { $"{prefix}Unauthorized", _UnauthorizedStatus },
        };
        return status;
    }

    public InfluxResilientStore(ILogger logger, IInfluxStore influxSimpleStore, IMeasurementStore store)
    {
        _logger = logger;
        _influxSimpleStore = influxSimpleStore;
        _store = store;
        _connectionTimer = new System.Timers.Timer(30000);
        _connectionTimer.Elapsed += OnTimedEvent;
        _connectionTimer.AutoReset = true;
        _connectionTimer.Enabled = true;
    }

    public void ProcessMeasurement(Measurement measurement)
    {
        WriteMeasurement(measurement);
    }

    public void WriteMeasurement(Measurement measurement)
    {
        _measurements.Enqueue(measurement);
        PlanTask();
    }

    public void WriteMeasurements(IEnumerable<Measurement> measurements)
    {
        foreach (var measurement in measurements)
            _measurements.Enqueue(measurement);
        PlanTask();
    }

    public bool CheckHealth()
    {
        StoreStatus.IsHealthy.Value = _influxSimpleStore.CheckHealth();
        return StoreStatus.IsHealthy.Value;
    }

    public bool Ping()
    {
        StoreStatus.IsPingSuccessful.Value = _influxSimpleStore.Ping();
        return StoreStatus.IsPingSuccessful.Value;
    }

    private void PlanTask()
    {
        lock (_lock)
        {
            var waitToCreateNewTask = WriteAfterMs * 2;
            if (_taskCount == 0 ||
                _taskCount < _maxTaskCount
                  && (DateTime.Now - _taskStartTime).TotalMilliseconds > waitToCreateNewTask)
            {
                _taskCount++;
                CreateTask();
            }
        }
    }

    private void CreateTask()
    {
        _taskStartTime = DateTime.Now;
        var task = _taskFactory.StartNew(
            DoWork,
            TaskCreationOptions.None);
    }

    private void DoWork()
    {
        if (!_recoveryInProgress && _influxConnected && _store.Count() > 0)
        {
            _recoveryInProgress = true;
            DoRecoveryWork();
            _recoveryInProgress = false;
        }
        DoWork(WriteAfterMs);
    }

    private void DoWork(int writeAfterMs)
    {
        _logger.LogDebug($"Start Task {Environment.CurrentManagedThreadId}");
        Thread.Sleep(writeAfterMs);
        try
        {
            WriteToInflux();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Unknown Error: {0}", ex.Message);
        }
        Interlocked.Decrement(ref _taskCount);
        _logger.LogDebug("Task done {0}", Environment.CurrentManagedThreadId);
    }

    private void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        if (_UnauthorizedStatus)
        {
            _connectionTimer.Enabled = false;
        }
        try
        {
            _influxConnected = _influxSimpleStore.CheckHealth();
            _logger.LogDebug("Influx healthy {0}", _influxConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error by check Influx health: {0}", ex.Message);
            _influxConnected = false;
        }
    }

    private void RemoveMeasurement(int? id)
    {
        if (id == null)
            return;
        try
        {
            var count = _store.Remove(id.Value);
            _logger.LogInformation("Remove measurement from store. Count {0} Id: {1}", count, id);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Cannot remove measurement from store: Id: {0}. Reason: {1}", id, ex.Message);
        }
    }

    private void DoRecoveryWork()
    {
        int? id = null;
        try
        {
            var storeCount = _store.Count();
            if (storeCount > 0)
            {
                _logger.LogInformation("Try to send measurements from the local error database to Influx. Count: {0}", storeCount);
                foreach (var measurement in _store.GetAll())
                {
                    _logger.LogDebug("Write single measurement.");
                    id = measurement.Id;
                    _influxSimpleStore.WriteMeasurement(measurement.ToMeasurement());
                    RemoveMeasurement(id);
                }
            }
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogCritical("Unauthorized: {0}", ex.Message);
            _UnauthorizedStatus = true;
        }
        catch (BadRequestException ex)
        {
            _logger.LogError("Invalid Payload: {0}", ex.Message);
            RemoveMeasurement(id);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid Payload: {0}", ex.Message);
            RemoveMeasurement(id);
        }
        catch (RestApiException ex)
        {
            _logger.LogError("Cannot write measurement to Influx: {0}", ex.Message);
            _influxConnected = false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unknown Error: {0}", ex.Message);
        }
    }

    private void WriteToInflux()
    {
        var currentCount = _measurements.Count;
        while (currentCount > 0 && _measurements.Count > 0)
        {
            var batch = new List<Measurement>();
            var x = BatchSize > 0 ? BatchSize : 1;
            _logger.LogDebug("Max batch size: {0} Measurement Count: {1}", x, _measurements.Count);
            while (currentCount-- > 0 && x > 0)
            {
                if (_measurements.TryDequeue(out var measurement))
                {
                    batch.Add(measurement);
                    x--;
                }
            }
            _logger.LogDebug("Current batch size: {0} Measurement Count: {1}", batch.Count, _measurements.Count);
            if (batch.Count > 0)
                WriteBatchToInflux(batch);
            //Thread.Sleep(500);
        }
    }

    private void WriteBatchToInflux(List<Measurement> measurements)
    {
        try
        {
            if (!_UnauthorizedStatus && _influxConnected)
            {
                _logger.LogInformation("Write measurements as batch. Count: {0}", measurements.Count);
                _influxSimpleStore.WriteMeasurements(measurements);
            }
            else
                SaveToStore(measurements);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogCritical("Unauthorized: {0}", ex.Message);
            _UnauthorizedStatus = true;
            SaveToStore(measurements);
        }
        catch (BadRequestException ex)
        {
            _logger.LogError("Invalid Payload: {0}", ex.Message);
            WriteToInfluxSeparately(measurements);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid Payload: {0}", ex.Message);
            WriteToInfluxSeparately(measurements);
        }
        catch (RestApiException ex)
        {
            _logger.LogError("Cannot write measurement to Influx: {0}", ex.Message);
            SaveToStore(measurements);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Cannot write measurement to Influx: {0}", ex.Message);
        }
    }

    private void WriteToInfluxSeparately(List<Measurement> measurements)
    {
        _logger.LogInformation("Write every measurement separately to Influx. Count: {0}", measurements.Count);
        measurements.ForEach(m => WriteToInflux(m));
    }

    private void WriteToInflux(Measurement measurement)
    {
        try
        {
            if (!_UnauthorizedStatus && _influxConnected)
            {
                _logger.LogDebug("Write single measurement.");
                _influxSimpleStore.WriteMeasurement(measurement);
            }
            else
                SaveToStore(measurement);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogCritical("Unauthorized: {0}", ex.Message);
            _UnauthorizedStatus = true;
            SaveToStore(measurement);
        }
        catch (BadRequestException ex)
        {
            _logger.LogError("Invalid Payload: {0} | {1}", ex.Message, measurement.ToString());
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid Payload: {0} | {1}", ex.Message, measurement.ToString());
        }
        catch (RestApiException ex)
        {
            _logger.LogError("Cannot write measurement to Influx: {0}", ex.Message);
            SaveToStore(measurement);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unknown Error: {0} | {1}", ex.Message, measurement.ToString());
        }
    }

    private void SaveToStore(Measurement measurement)
    {
        try
        {
            var count = _store.Save(measurement);
            InfluxErrorCount += count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    private void SaveToStore(List<Measurement> measurements)
    {
        try
        {
            var count = _store.Save(measurements);
            InfluxErrorCount += count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public void Dispose()
    {
    }
}