using HA.Influx;
using HA.Nats;
using HA.Store;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using System.Text;

namespace HA.InfluxWriter.Service;

public class Components
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    private ConsoleObserver? _consoleObserver;

    public Components(ILoggerFactory loggerFactory, AppSettings settings)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<Components>();
        _settings = settings;
        InitComponents(settings);
    }

    public QueueConsumerWorker? QueueMeasurmentObservable { get; private set; }

    public InfluxResilientStore? InfluxResilientStore { get; private set; }

    public MeasurementObserver? InfluxMeasurementObserver { get; private set; }

    public FileStoreObserver? FileStoreObserver { get; private set; }

    public void EnableConsoleObserver()
    {
        _consoleObserver = new ConsoleObserver();
        QueueMeasurmentObservable?.Subscribe(_consoleObserver);
    }

    public string CurrentStatus()
    {
        var sb = new StringBuilder();
        if (QueueMeasurmentObservable != null && InfluxResilientStore != null && InfluxMeasurementObserver != null)
        {
            var onNextCount = QueueMeasurmentObservable.OnNextCount;
            // initialized
            sb.AppendLine($"Measurement received [count/15min]: {onNextCount.DurationCount.ToString()} ");
            sb.AppendLine($"Last change: {onNextCount.LastChangeTime}");
            //sb.Append($"Last Event sent: {(DateTime.Now - QueueMeasurmentObservable.LastOnNext).TotalSeconds} s ");
            sb.AppendLine($"[Measurement] Last Proccessed: {(DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds} s ");
            sb.AppendLine($"[Influx] Queue Count: {InfluxResilientStore.QueueCount} Error Count: {InfluxResilientStore.InfluxErrorCount} ");
        }
        else
        {
            sb.AppendLine("Status: Not Ready, components un-initialized");
        }
        return sb.ToString();
    }

    /*public IDictionary<string, string> CurrentComponentsStatus()
    {
        var status = new Dictionary<string, string>();
        if (QueueMeasurmentObservable != null && InfluxResilientStore != null && InfluxMeasurementObserver != null)
        {
            var root = "health/kostalservice/";
            status.Add($"{root}lastHeartBeat", DateTime.Now.ToString("o"));
            status.Add($"{root}observable/lastMeasurementSec",
                (DateTime.Now - QueueMeasurmentObservable.LastOnNext).TotalSeconds.ToString("#.000"));
            status.Add($"{root}influx/lastMeasurementStoredSec",
                (DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds.ToString("#.000"));
            status.Add($"{root}influx/queueCount", InfluxResilientStore.QueueCount.ToString());
            status.Add($"{root}influx/totalErrorCount", InfluxResilientStore.InfluxErrorCount.ToString());
        }
        return status;
    }*/

    private void InitComponents(AppSettings appSettings)
    {
        var natsSettings = appSettings.Nats;
        var natsOpts = new NatsOpts
        {
            Name = natsSettings.ClientName,
            Url = natsSettings.Url,
            Verbose = false,
            AuthOpts = new NatsAuthOpts
            {
                Username = natsSettings.User,
                Password = natsSettings.Password
            }
        };
        QueueMeasurmentObservable = new QueueConsumerWorker(
            _loggerFactory.CreateLogger<QueueConsumerWorker>(),
            natsOpts);
        var influxSettings = appSettings.Influx;

        InfluxResilientStore = new InfluxResilientStore(
            _loggerFactory.CreateLogger<InfluxResilientStore>(),
            new InfluxSimpleStore(
                influxSettings.InfluxUrl,
                influxSettings.InfluxBucket,
                influxSettings.InfluxOrg,
                influxSettings.InfluxToken),
            new MeasurementStore(appSettings.Application.StoreFilePath));

        InfluxMeasurementObserver = new MeasurementObserver(
                _loggerFactory.CreateLogger<MeasurementObserver>(),
                InfluxResilientStore);
        FileStoreObserver = new FileStoreObserver(
                _loggerFactory.CreateLogger<FileStoreObserver>(),
                new FileStore(_loggerFactory.CreateLogger<FileStore>(),
                "Measurements"));

        InfluxMeasurementObserver?.Subscribe(QueueMeasurmentObservable);
        FileStoreObserver?.Subscribe(QueueMeasurmentObservable);
    }
}