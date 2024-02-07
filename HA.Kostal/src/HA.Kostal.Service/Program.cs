using HA.Influx;
using HA.Nats;
using HA.Service.Settings;
using HA.Store;

namespace HA.Kostal.Service;

public class Program
{
    private static ILogger? _logger;

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
        var appCommonSettings = new AppInitSettings();
        var loggerFactory = LoggerFactory.Create(builder => builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("HA", appCommonSettings.LoggingLevel)
            .SetMinimumLevel(appCommonSettings.LoggingLevel)
            .AddSimpleConsole(options => {
                options.SingleLine = true;
                options.TimestampFormat = "dd.MM HH:mm:ss ";
            }));
        _logger = loggerFactory.CreateLogger<Program>();
        _logger.LogInformation("start Kostal service");

        try
        {
            var appSettings = new AppSettings(
                loggerFactory.CreateLogger<AppSettings>(),
                appCommonSettings);
            appSettings.CheckSettings();

            var kostalObservable = new KostalObservable(
                loggerFactory.CreateLogger<KostalObservable>(),
                new KostalClient(
                    appSettings.Kostal.KostalUrl,
                    appSettings.Kostal.KostalUser, 
                    appSettings.Kostal.KostalPassword));
            kostalObservable.StopDuringSunset = appSettings.Kostal.KostalStopDuringSunset;

            var influxResilientStore = new InfluxResilientStore(
                loggerFactory.CreateLogger<InfluxResilientStore>(),
                new InfluxSimpleStore(
                    appSettings.Influx.InfluxUrl,
                    appSettings.Influx.InfluxBucket,
                    appSettings.Influx.InfluxOrg,
                    appSettings.Influx.InfluxToken),
                new MeasurementStore(appSettings.Application.StoreFilePath));
            var influxMeasurementObserver = new MeasurementObserver(
                loggerFactory.CreateLogger<MeasurementObserver>(),
                influxResilientStore);

            var natsPublisher = new NatsPublisher(
                loggerFactory.CreateLogger<NatsPublisher>(),
                new NatsOptions(
                    appSettings.Nats.Url,
                    appSettings.Nats.ClientName,
                    appSettings.Nats.User,
                    appSettings.Nats.Password));
            var natsPublisherProcessor = new NatsPublisherProcessor(
                loggerFactory.CreateLogger<MeasurementObserver>(),
                natsPublisher);
            var natsMeasurementObserver = new MeasurementObserver(
                loggerFactory.CreateLogger<MeasurementObserver>(),
                natsPublisherProcessor);

            influxMeasurementObserver.Subscribe(kostalObservable);
            natsMeasurementObserver.Subscribe(kostalObservable);
            
            var consoleObserver = new ConsoleObserver();
            kostalObservable?.Subscribe(consoleObserver);

            var components = new Components(loggerFactory, appSettings) { 
                KostalObservable = kostalObservable,
                InfluxResilientStore = influxResilientStore,
                InfluxMeasurementObserver = influxMeasurementObserver,
                NatsMeasurementObserver = natsMeasurementObserver
            };

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddSingleton(components)
                    .AddSingleton(loggerFactory.CreateLogger<Worker>())
                    .AddHostedService<Worker>())
                .Build();
            host.Run();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, $"Stopped program because of exception: {ex.Message}");
            throw;
        }
    }

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _logger?.LogCritical(ex, ex?.Message);
    }
}