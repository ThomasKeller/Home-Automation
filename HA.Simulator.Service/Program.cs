using HA.Nats;
using HA.Service;
using HA.Service.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

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
                options.TimestampFormat = "yy-MM-dd HH:mm:ss.fff ";
            }));

        try
        {
            var appSettings = new AppSettings(
                loggerFactory.CreateLogger<AppSettings>(),
                appInitSettings);
            appInitSettings.CheckSettings();

            _logger = loggerFactory.CreateLogger<Program>();
            _logger.LogInformation("start service");

            var natsOpts = new NatsOpts
            {
                Url = appSettings.Nats.Url,
                AuthOpts = new NatsAuthOpts
                {
                    Username = appSettings.Nats.User,
                    Password = appSettings.Nats.Password
                }
            };
            var natsPublisher = new NatsPublisher(loggerFactory.CreateLogger<NatsPublisher>(), natsOpts);
            var natsWorker = new NatsWorker(loggerFactory.CreateLogger<NatsWorker>(), natsPublisher);
            var natsTask = natsWorker.StartAsync(CancellationToken.None);
            var natsObserver = new MeasurementObserver(loggerFactory.CreateLogger<Measurement>(), natsWorker);
            var simulatorObservableWorker = new SimulatorObservableWorker(loggerFactory.CreateLogger<SimulatorObservableWorker>());
            simulatorObservableWorker.SleepTimeMs = 10000;
            natsObserver.Subscribe(simulatorObservableWorker.MeasurementObservable);
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
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

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _logger?.LogCritical(ex, ex?.Message);
    }
}