using HA.Observable;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HA.Service;

public class SimulatorObservableWorker : BackgroundService
{
    private readonly ILogger _logger;
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public class SimulatorMeasurementObservable : ObservableBase<Measurement>
    {
    }

    public SimulatorObservableWorker(ILogger<SimulatorObservableWorker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public uint SleepTimeMs { get; set; } = 1000; 
        
    public SimulatorMeasurementObservable MeasurementObservable { get; private set; } = new SimulatorMeasurementObservable();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var machineName = Environment.MachineName;
        var osVersion = Environment.OSVersion.ToString();
        var version = Environment.Version.ToString();
        var counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var measurement = new Measurement()
            {
                Device = machineName,
                Quality = QualityInfos.Good,
            };
            measurement.Tags.Add("MachineName", machineName);
            measurement.Tags.Add("OSVersion", osVersion);
            measurement.Tags.Add("Version", version);
            measurement.Values.Add(MeasuredValue.Create("UpTimeHour", TimeSpan.FromMilliseconds(Environment.TickCount64).TotalHours));
            measurement.Values.Add(MeasuredValue.Create("Counter", counter++));
            _logger.LogInformation("{0} {1}", ThreadIdString , measurement.ToString());
            MeasurementObservable.ExecuteOnNext(measurement);
            if (counter > 25)
                counter = 0;
            await Task.Delay((int)SleepTimeMs, stoppingToken);
        }
    }
}
