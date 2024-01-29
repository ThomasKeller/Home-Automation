using HA.AppTools;
using HA.Nats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace HA.InfluxWriter.Service;

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

            /*var natsOpts = new NatsOpts() {
                Name = appSettings.Nats.Name,
                Url = appSettings.Nats.Url,
                AuthOpts = new NatsAuthOpts() { 
                    Username=appSettings.Nats.User,
                    Password=appSettings.Nats.Password },
                LoggerFactory = loggerFactory,
                Verbose = false,
            };
            var streamCreator = new NatsStreamCreator(loggerFactory.CreateLogger<NatsStreamCreator>())
            {
                                
            };
            var streamExists = await streamCreator.CreateStreamAsync(natsOpts);
            */



            var components = new Components(loggerFactory, appSettings);
            components.EnableConsoleObserver();

            _logger = loggerFactory.CreateLogger<Program>();
            _logger.LogInformation("start Kostal service");
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