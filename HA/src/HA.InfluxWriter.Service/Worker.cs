using HA.Influx;
using HA.InfluxWriter.Service;
using HA.Nats;
using HA.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace HA.InfluxWriter.Service;

public class Worker : BackgroundService
{
    private const string LogCategoryInfluxStore   = "InfluxStore   ";
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
        _logger.LogInformation("{0} Create Influx Resilient Store", ThreadIdString);
        var influxResilientStore = CreateInfluxResilientStore();
        _logger.LogInformation("{0} Create Nats Subscriber", ThreadIdString);
        var natsSubscriber = CreateNatsSubscriber();
        _logger.LogInformation("{0} Create Measurement Observer", ThreadIdString);
        var measurementObserver = new MeasurementObserver(
            _loggerFactory.CreateLogger(LogCategoryObserver),
            influxResilientStore);
        measurementObserver.Subscribe(natsSubscriber);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("{0} Subscribe Nats Subject: '{1}' Queue Group: {2}",
                ThreadIdString, _appSettings.NatsConsumer.FilteredSubject, _appSettings.NatsConsumer.QueueGroup);
            await natsSubscriber.SubscibeAsync(
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
                    ? "HA.InfluxWriter.Service" 
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
                filteredSubject: _appSettings.NatsConsumer.FilteredSubject) 
                {
                    StreamName = _appSettings.NatsConsumer.StreamName,
                    ConsumerName = _appSettings.NatsConsumer.ConsumerName,
                    QueueGroup = _appSettings.NatsConsumer.QueueGroup,
                });
    }

    private InfluxResilientStore CreateInfluxResilientStore()
    {
        return new InfluxResilientStore(
            _loggerFactory.CreateLogger(LogCategoryInfluxStore),
            new InfluxSimpleStore(
                _appSettings.Influx.InfluxUrl,
                _appSettings.Influx.InfluxBucket,
                _appSettings.Influx.InfluxOrg,
                _appSettings.Influx.InfluxToken),
                new MeasurementStore(_appSettings.Application.StoreFilePath));
    }
}