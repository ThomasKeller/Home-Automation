using HA.Influx;
using HA.Mqtt;
using HA.Store;
using HA.EhZ.Observable;
using System.Text;
using HA.EhZ.Observer;
using System.Net;
using HA.Service.Settings;

namespace HA.EhZ.Service;

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
        if (settings.Ehz.UseSerialPort)
        {
            InitSerialPortComponent(settings.Ehz);
        }
        else
        {
            InitUdpInComponent(settings.Ehz);
        }
        InitInfluxComponent(settings.Influx, settings.Application.StoreFilePath);
        InitMqttComponent(settings.Mqtt);
    }

    public UdpClientObservable? UdpClientObservable { get; private set; }

    public SerialPortObservable? ComPortObservable { get; private set; }

    public ParserObservable? ParserObservable { get; private set; }

    public InfluxResilientStore? InfluxResilientStore { get; set; }

    public MeasurementObserver? InfluxMeasurementObserver { get; set; }

    public MeasurementObserver? MqttMeasurementObserver { get; set; }

    public UdpServerObserver? UdpServerObserver { get; private set; }

    public MqttPublisher? HealthMqttPublisher { get; private set; }

    public void EnableConsoleObserver()
    {
        _consoleObserver = new ConsoleObserver();
        ParserObservable?.Subscribe((IObserver<Measurement>)_consoleObserver);
    }

    public string CurrentStatus()
    {
        var sb = new StringBuilder();
        if ((ComPortObservable != null || UdpClientObservable != null) &&
            ParserObservable != null && InfluxResilientStore != null &&
            InfluxMeasurementObserver != null)
        {
            // initialized
            if (ComPortObservable != null) 
                sb.Append($"Last Bytes received: {(DateTime.Now - ComPortObservable.LastBytesSentAt).TotalSeconds} s ");
            sb.Append($"[Measurement] Last Proccessed: {(DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds} s ");
            sb.Append($"[Influx] Queue Count: {InfluxResilientStore.QueueCount} Error Count: {InfluxResilientStore.InfluxErrorCount} ");
        }
        else
        {
            sb.AppendLine("Status: Not Ready, components un-initialized");
        }
        return sb.ToString();
    }

    public IDictionary<string, string> CurrentComponentsStatus()
    {
        var status = new Dictionary<string, string>();
        if (ParserObservable != null &&
            InfluxResilientStore != null && InfluxMeasurementObserver != null)
        {
            var root = "health/ehzservice/";
            status.Add($"{root}lastHeartBeat", DateTime.Now.ToString("o"));
            status.Add($"{root}observable/lastMeasurementSec",
                (DateTime.Now - ParserObservable.LastMeasurementSentAt).TotalSeconds.ToString("#.000"));
            status.Add($"{root}influx/lastMeasurementStoredSec",
                (DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds.ToString("#.000"));
            status.Add($"{root}influx/queueCount", InfluxResilientStore.QueueCount.ToString());
            status.Add($"{root}influx/totalErrorCount", InfluxResilientStore.InfluxErrorCount.ToString());
        }
        return status;
    }

    private void InitSerialPortComponent(EhZSettings settings)
    {
        ComPortObservable = new SerialPortObservable(
            _loggerFactory.CreateLogger<SerialPortObservable>(),
            settings.SerialPort);
        ParserObservable = new ParserObservable(
            _loggerFactory.CreateLogger<ParserObservable>(),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(10));
        ComPortObservable.Subscribe(ParserObservable);
        if (settings.EnableUdpServer)
        {
            UdpServerObserver = new UdpServerObserver(
                _loggerFactory.CreateLogger<UdpServerObserver>(),
                IPAddress.Broadcast.ToString(),
                settings.UdpPortOut);
            ComPortObservable.Subscribe(UdpServerObserver);
        }
        if (!string.IsNullOrEmpty(settings.LineprotocolLogPath))
        {
            var filestore = new FileStore(
                _loggerFactory.CreateLogger<FileStore>(),
                settings.LineprotocolLogPath);
            var fileStoreObserver = new FileStoreObserver(
                    _loggerFactory.CreateLogger<FileStoreObserver>(),
                    filestore);
            ParserObservable.Subscribe(fileStoreObserver);
        }
    }

    private void InitUdpInComponent(EhZSettings settings)
    {
        UdpClientObservable = new UdpClientObservable(
            _loggerFactory.CreateLogger<SerialPortObservable>(),
            settings.UdpPortIn);
        ParserObservable = new ParserObservable(
            _loggerFactory.CreateLogger<ParserObservable>(),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(10));
        UdpClientObservable.Subscribe(ParserObservable);
    }

    private void InitInfluxComponent(InfluxSettings settings, string measurmentStoreFilePath)
    {
        InfluxResilientStore = new InfluxResilientStore(
            _loggerFactory.CreateLogger<InfluxResilientStore>(),
            new InfluxSimpleStore(
                settings.InfluxUrl,
                settings.InfluxBucket,
                settings.InfluxOrg,
                settings.InfluxToken),
            new MeasurementStore(measurmentStoreFilePath));
        InfluxMeasurementObserver = new MeasurementObserver(
                _loggerFactory.CreateLogger<MeasurementObserver>(),
                InfluxResilientStore);
        if (ParserObservable != null)
            InfluxMeasurementObserver.Subscribe(ParserObservable);
    }

    private void InitMqttComponent(MqttSettings settings)
    {
        var mqttPublisher = new MqttPublisher(
            _loggerFactory.CreateLogger<MqttPublisher>(),
            settings.MqttHost,
            settings.MqttPort,
            settings.MqttClientId);
        HealthMqttPublisher = mqttPublisher;
        MqttMeasurementObserver = new MeasurementObserver(
               _loggerFactory.CreateLogger<MeasurementObserver>(),
               mqttPublisher);
        if (ComPortObservable != null && ParserObservable != null)
            MqttMeasurementObserver.Subscribe(ParserObservable);
        if (UdpClientObservable != null && ParserObservable != null)
            MqttMeasurementObserver.Subscribe(ParserObservable);
    }
}