using HA.Common;
using HA.Common.Influx;
using HA.Common.Mqtt;
using HA.Common.Redis;
using HA.Common.Store;
using HA.IOBroker.config;
using Newtonsoft.Json;
using System.Text;

namespace HA.IOBroker.Service
{
    public class Components
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private ConsoleObserver? _consoleObserver;

        public Components(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<Components>();
        }

        public string MeasurmentStorePath { get; set; } = Path.Combine("store", "iobroker.db");

        public IoBrokerRedisObservable? IoBrokerRedisObservable { get; private set; }

        public RedisClient? RedisClient { get; set; }

        public InfluxResilientStore? InfluxResilientStore { get; set; }

        public MeasurementObserver? InfluxMeasurementObserver { get; set; }

        public MqttPublisher? HealthMqttPublisher { get; private set; }

        public void Init(string workDir = "")
        {
            var appSettings = new AppSettings(workDir);
            appSettings.Read();

            InitIoBrokerRedisComponent(appSettings);
            InitInfluxComponents(appSettings);
            InitMqttComponent(appSettings);
        }

        public void EnableConsoleObserver()
        {
            _consoleObserver = new ConsoleObserver();
            IoBrokerRedisObservable?.Subscribe(_consoleObserver);
        }

        public string CurrentStatus()
        {
            var sb = new StringBuilder();
            if (IoBrokerRedisObservable != null && RedisClient != null
                && InfluxResilientStore != null && InfluxMeasurementObserver != null)
            {
                // initisalized
                sb.Append($"[Redis Client] Status: {RedisClient.Status.ToString()} Connected: {RedisClient.IsConnected} ");
                sb.Append($"[Observable] Event Queue Count: {IoBrokerRedisObservable.EventQueueCount} ");
                sb.Append($"Last Event sent: {(DateTime.Now - IoBrokerRedisObservable.LastMeasurementSentAt).TotalSeconds} s");
                sb.Append($"[Measurement] Last Proccessed: {(DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds} s");
                sb.Append($"[Influx] Queue Count: {InfluxResilientStore.QueueCount} Error Count: {InfluxResilientStore.InfluxErrorCount}");
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
            if (IoBrokerRedisObservable != null && InfluxResilientStore != null && InfluxMeasurementObserver != null)
            {
                var root = "health/iobrokerservice/";
                status.Add($"{root}lastHeartBeat", DateTime.Now.ToString("o"));
                status.Add($"{root}observable/lastMeasurementSec",
                    (DateTime.Now - IoBrokerRedisObservable.LastMeasurementSentAt).TotalSeconds.ToString("#.000"));
                status.Add($"{root}influx/lastMeasurementStoredSec",
                    (DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds.ToString("#.000"));
                status.Add($"{root}influx/queueCount", InfluxResilientStore.QueueCount.ToString());
                status.Add($"{root}influx/totalErrorCount", InfluxResilientStore.InfluxErrorCount.ToString());
            }
            return status;
        }

        private void InitIoBrokerRedisComponent(AppSettings appSettings)
        {
            var ioBrokerConfig = ReadDeviceConfig(appSettings);
            IoBrokerRedisObservable = new IoBrokerRedisObservable(
                _loggerFactory.CreateLogger<IoBrokerRedisObservable>(),
                ioBrokerConfig.BuildBindings().ToList());

            RedisClient = new RedisClient(
                _loggerFactory.CreateLogger<RedisClient>(),
                appSettings.RedisHost,
                appSettings.RedisPort);
            RedisClient.Subscriber.Subscribe("*", IoBrokerRedisObservable.OnEvent);

            _logger.LogInformation("Redis connected: {0} Client: {1}, Status: {2}",
                RedisClient.Subscriber.IsConnected("*"),
                RedisClient.ClientName,
                RedisClient.Status);
        }

        private void InitInfluxComponents(AppSettings appSettings)
        {
            InfluxResilientStore = new InfluxResilientStore(
                _loggerFactory.CreateLogger<InfluxResilientStore>(),
                new InfluxSimpleStore(
                    appSettings.InfluxUrl,
                    appSettings.InfluxBucket,
                    appSettings.InfluxOrg,
                    appSettings.InfluxToken),
                new MeasurementStore(MeasurmentStorePath));
            InfluxMeasurementObserver = new MeasurementObserver(
                    _loggerFactory.CreateLogger<MeasurementObserver>(),
                    InfluxResilientStore);
            if (IoBrokerRedisObservable != null)
                InfluxMeasurementObserver.Subscribe(IoBrokerRedisObservable);
        }

        private static IoBrokerConfig ReadDeviceConfig(AppSettings appSettings)
        {
            var deviceConfig = File.ReadAllText(appSettings.DeviceConfigFilePath);
            var ioBrokerConfig = JsonConvert.DeserializeObject<IoBrokerConfig>(deviceConfig);
            Console.WriteLine("Device groups:");
            foreach (var group in ioBrokerConfig.DeviceGroups)
                Console.WriteLine($"    Name: {group.GroupName} Properties: {string.Join(", ", group.Properties.Keys)}");
            Console.WriteLine();
            Console.WriteLine("Devices:");
            foreach (var device in ioBrokerConfig.Devices)
                Console.WriteLine($"    Name: {device.DeviceName} Id: {device.DeviceId} Group: {device.GroupName}");
            return ioBrokerConfig;
        }

        private void InitMqttComponent(AppSettings appSettings)
        {
            var mqttPublisher = new MqttPublisher(
                _loggerFactory.CreateLogger<MqttPublisher>(),
                appSettings.MqttHost,
                appSettings.MqttPort,
                "ha.iobroker.service");
            HealthMqttPublisher = mqttPublisher;
        }
    }
}