using Microsoft.Extensions.Logging;

namespace HA.Nats
{
    public class NatsSimpleStore : IObserverProcessor
    {
        private readonly ILogger _logger;
        private readonly IMeasurmentStore _natsStore;

        public NatsSimpleStore(ILogger logger, IMeasurmentStore measurementStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _natsStore = measurementStore ?? throw new ArgumentNullException(nameof(measurementStore));
        }

        public void ProcessMeasurement(Measurement measurement)
        {
            _natsStore.WriteMeasurement(measurement);
        }
    }
}
