using Microsoft.Extensions.Logging;

namespace HA.Nats;

public class NatsPublisherProcessor : IObserverProcessor
{
    private readonly ILogger _logger;
    private readonly NatsPublisher _natsPublisher;

    public NatsPublisherProcessor(ILogger logger, NatsPublisher natsPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _natsPublisher = natsPublisher ?? throw new ArgumentNullException(nameof(natsPublisher)); 
    }

    public bool AddDeviceToSubject { get; set; } = true;

    public string Subject { get; set; } = "measurements.new";

    public void ProcessMeasurement(Measurement measurement)
    {
        if (measurement != null)
        {
            var subject = AddDeviceToSubject ? $"{Subject}.{measurement.Device}" : Subject;
            _logger.LogDebug("Process measurement: Subject: {0} Measurement: {1}", subject, measurement.ToString());
            AsyncHelper.RunSync(() => _natsPublisher.PublishAsync(subject, measurement));
        }
    }
}
