using HA.AppTools;

namespace HA.EhZ.Service;

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

            var components = new Components(loggerFactory, appSettings);
            //components.Init(envWorkDir);
            //components.EnableConsoleObserver();

            _logger = loggerFactory.CreateLogger<Program>();
            _logger.LogInformation("start service");
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