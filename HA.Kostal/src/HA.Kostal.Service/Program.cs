using HA.Nats;
using HA.Service;
using HA.Service.Settings;

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

            _logger.LogInformation("Create Kostal Subject");
            var kostalObservable = new KostalObservable(
                loggerFactory.CreateLogger("KostalObservable"),
                new KostalClient(
                appSettings.Kostal.KostalUrl,
                appSettings.Kostal.KostalUser,
                appSettings.Kostal.KostalPassword)) {
                StopDuringSunset = appSettings.Kostal.KostalStopDuringSunset,
                Longtitude = appSettings.Kostal.Longtitude,
                Latitude = appSettings.Kostal.Latitude,
                MeasureInterval = TimeSpan.FromSeconds(appSettings.Kostal.MeasureInterval_s),
                SleepInterval = TimeSpan.FromMinutes(appSettings.Kostal.SleepInterval_min)
            };
            var natsPublisher = new NatsPublisher(
                loggerFactory.CreateLogger("NatsPublisher"),
                appSettings.Nats.CreateNatsOpts(),
                !string.IsNullOrEmpty(appSettings.NatsStream.StreamName));

            var natsPublisherWorker = new NatsPublisherWorker(
                loggerFactory.CreateLogger("NatsPublisherWorker"),
                natsPublisher,
                appSettings.NatsStream.SubjectPrefix);

            var measurmentObserver = new MeasurementObserver(
                loggerFactory.CreateLogger("MeasurementObserver"),
                natsPublisherWorker);

            var disposable = kostalObservable.Subscribe(measurmentObserver);

            var task1 = natsPublisherWorker.CreateTaskAsync(CancellationToken.None);
            var task2 = kostalObservable.ReadFromKostalAsync(CancellationToken.None);

            AsyncHelper.RunSync(() => Task.WhenAll(task1, task2));
            Console.ReadLine();
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