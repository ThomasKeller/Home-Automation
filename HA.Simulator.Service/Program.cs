using HA.Nats;
using HA.Service;
using HA.Service.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HA.Simulator.Service;

public class Program
{
    private static ILogger? _logger;

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
        var appInitSettings = new AppInitSettings();
        var loggerFactory = LoggerFactory.Create(builder => builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("ha", appInitSettings.LoggingLevel)
            .SetMinimumLevel(appInitSettings.LoggingLevel)
            .AddSimpleConsole(options => {
                options.SingleLine = true;
                options.TimestampFormat = "dd.MM HH:mm:ss ";
            }));

        try
        {
            var appSettings = new AppSettings(
                loggerFactory.CreateLogger<AppSettings>(),
                appInitSettings);
            appInitSettings.CheckSettings();

            _logger = loggerFactory.CreateLogger<Program>();
            _logger.LogInformation("start Simulator Service");
            var streamAvailable = CreateStreamAsync(loggerFactory, appSettings).Result;

            var natsPublisher = new NatsPublisher(loggerFactory.CreateLogger<NatsPublisher>(), appSettings.CreateNatsOpts());
            var natsWorker = new NatsPublisherWorker(loggerFactory.CreateLogger<NatsPublisherWorker>(), natsPublisher);
            var natsTask = natsWorker.StartAsync(CancellationToken.None);
            var natsObserver = new MeasurementObserver(loggerFactory.CreateLogger<Measurement>(), natsWorker);
            var simulatorObservableWorker = new SimulatorObservableWorker(loggerFactory.CreateLogger<SimulatorObservableWorker>());
            simulatorObservableWorker.SleepTimeMs = 10000;
            natsObserver.Subscribe(simulatorObservableWorker.MeasurementObservable);
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddSingleton(loggerFactory)
                    .AddHostedService(sow => simulatorObservableWorker))
                .Build();
            host.Run();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Stopped program because of exception");
            throw;
        }
    }

    private static async Task<bool> CreateStreamAsync(ILoggerFactory loggerFactory, AppSettings appSettings)
    {
        var streamName = appSettings.NatsStream.StreamName;
        var subject = appSettings.NatsStream.Subject;
        var maxAgeInDays = appSettings.NatsStream.maxAgeInDays;
        var streamEnable = !string.IsNullOrEmpty(streamName) && !string.IsNullOrEmpty(subject) && maxAgeInDays > 0;
        var topicPrefix = appSettings.NatsStream.TopicPrefix;
        var natsUtils = new NatsUtils(loggerFactory.CreateLogger("NatsUtils  "));
        var connection = await natsUtils.CreateConnectionAsync(appSettings.CreateNatsOpts(), 5, 5);
        if (streamEnable)
        {
            var streamItems = await natsUtils.CreateStreamAsync(connection, streamName, subject, maxAgeInDays);
            if (streamItems.Context == null)
                throw new ArgumentNullException("No nats context available");
            var isAvailable = await natsUtils.CheckStreamExistAsync(streamItems.Context, streamName);
            return isAvailable;
        }
        return false;
    }

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _logger?.LogCritical(ex, ex?.Message);
    }
}