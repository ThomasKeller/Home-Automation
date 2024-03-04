using HA.Service.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                options.TimestampFormat = "dd.MM HH:mm:ss ";
            }));
        try
        {
            _logger = loggerFactory.CreateLogger("Program       ");
            _logger.LogInformation("start InfluxWriter service");
            var appSettings = new AppSettings(
                loggerFactory.CreateLogger("AppSettings   "),
                appInitSettings);
            appInitSettings.CheckSettings();
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddSingleton(lf => loggerFactory)
                    .AddSingleton(appSettings => appSettings)
                    .AddHostedService(w => new Worker(loggerFactory, appSettings)))
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