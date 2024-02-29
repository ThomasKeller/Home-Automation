using HA.Nats;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace HA.MqttPublisher.Service;

public class Worker : BackgroundService
{
    private const string LogCategoryMqttPublisher = "MqttPublisher ";
    private const string LogCategoryWorker        = "Worker        ";
    private const string LogCategoryObserver      = "Meas.Observer ";
    private const string LogCategoryNatsSubcriber = "NatsSubscriber";
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly AppSettings _appSettings;
    private string ThreadIdString => $"[TID:{Thread.CurrentThread.ManagedThreadId}]";

    public Worker(ILoggerFactory loggerFactory, AppSettings appSettings)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(LogCategoryWorker);
        _appSettings = appSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastLog = DateTime.MinValue;
        _logger.LogInformation("{0} Create MQTT Publisher", ThreadIdString);
        var mqttPublisher = CreateMqttPublisher();
        _logger.LogInformation("{0} Create Nats Subscriber", ThreadIdString);
        var natsSubscriber = CreateNatsSubscriber();
        _logger.LogInformation("{0} Create Measurement Observer", ThreadIdString);
        var measurementObserver = new MeasurementObserver(
            _loggerFactory.CreateLogger(LogCategoryObserver),
            mqttPublisher);
        measurementObserver.Subscribe(natsSubscriber);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("{0} Subscribe Nats Subject: '{1}' Queue Group: {2}",
                ThreadIdString, _appSettings.NatsConsumer.FilteredSubject, _appSettings.NatsConsumer.QueueGroup);
            await natsSubscriber.SubscribeAsync(
                _appSettings.NatsConsumer.FilteredSubject,
                _appSettings.NatsConsumer.QueueGroup,
                stoppingToken: stoppingToken);
            _logger.LogDebug("{0} Wait", ThreadIdString);
            await Task.Delay(1000, stoppingToken);
        }
    }

    private NatsOpts CreateNatsOpts()
    {
        return new NatsOpts
        {
            Url = _appSettings.Nats.Url,
            Name = string.IsNullOrEmpty(_appSettings.Nats.ClientName) 
                    ? "HA.MqttPublisher.Service" 
                    : _appSettings.Nats.ClientName,
            AuthOpts = new NatsAuthOpts {
                Username = _appSettings.Nats.User,
                Password = _appSettings.Nats.Password
            }
        };
    }

    private NatsSubscriber CreateNatsSubscriber()
    {
        return new NatsSubscriber(
            _loggerFactory.CreateLogger(LogCategoryNatsSubcriber),
            new NatsSubscriber.Parameters(
                CreateNatsOpts(),
                filteredSubject: _appSettings.NatsConsumer.FilteredSubject));
    }

    private Mqtt.MqttPublisher CreateMqttPublisher()
    {
        return new Mqtt.MqttPublisher(
            _loggerFactory.CreateLogger(LogCategoryMqttPublisher),
            _appSettings.Mqtt.MqttHost,
            _appSettings.Mqtt.MqttPort,
            _appSettings.Mqtt.MqttClientId);
    }
}