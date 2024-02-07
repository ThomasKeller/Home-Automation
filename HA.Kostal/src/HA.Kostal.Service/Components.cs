using HA.Influx;
using HA.Mqtt;
using HA.Store;
using System.Text;

namespace HA.Kostal.Service
{
    public record Components
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppSettings _settings;
        private ConsoleObserver? _consoleObserver;

        public Components(ILoggerFactory loggerFactory, AppSettings settings)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _settings = settings;
        }

        public KostalObservable? KostalObservable { get; set; }

        public InfluxResilientStore? InfluxResilientStore { get; set; }

        public MeasurementObserver? InfluxMeasurementObserver { get; set; }

        public MeasurementObserver? NatsMeasurementObserver { get; set; }

        public string CurrentStatus()
        {
            var sb = new StringBuilder();
            if (KostalObservable != null && InfluxResilientStore != null && InfluxMeasurementObserver != null)
            {
                // initialized
                sb.Append($"Last Event sent: {(DateTime.Now - KostalObservable.LastMeasurementSentAt).TotalSeconds} s ");
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
            if (KostalObservable != null && InfluxResilientStore != null && InfluxMeasurementObserver != null)
            {
                var root = "health/kostalservice/";
                status.Add($"{root}lastHeartBeat", DateTime.Now.ToString("o"));
                status.Add($"{root}observable/lastMeasurementSec",
                    (DateTime.Now - KostalObservable.LastMeasurementSentAt).TotalSeconds.ToString("#.000"));
                status.Add($"{root}influx/lastMeasurementStoredSec",
                    (DateTime.Now - InfluxMeasurementObserver.LastMeasurementProccessed).TotalSeconds.ToString("#.000"));
                status.Add($"{root}influx/queueCount", InfluxResilientStore.QueueCount.ToString());
                status.Add($"{root}influx/totalErrorCount", InfluxResilientStore.InfluxErrorCount.ToString());
            }
            return status;
        }
    }
}