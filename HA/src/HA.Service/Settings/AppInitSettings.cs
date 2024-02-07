using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HA.Service.Settings;

public class AppInitSettings : AppSettingsBase
{
    public class DefaultSettings
    {
        public string SettingsFolderName { get; set; } = "settings";
        public string SettingsFileName { get; set; } = "appsettings.json";
        public string StoreFileName { get; set; } = "measurements.db";
        public string StoreFolderName { get; set; } = "store";
    }
    private readonly string _storeFilePath;
    private readonly string _settingsFilePath;
    private readonly string _workingDirectory;

    public DefaultSettings Settings { get; private set; } = new DefaultSettings();

    public string StoreFilePath => _storeFilePath;
    public string SettingsFilePath => _settingsFilePath;

    [EnvParameter("LOGGINGLEVEL")]
    [ConfigParameter("Common", "LoggingLevel")]
    public string LoggingLevelString { get; set; } = "Information";

    public LogLevel LoggingLevel => ParseLogginLevelString();

    public string WorkingDirectory => _workingDirectory;

    public bool SettingsFileExists => Configuration != null;

    public IConfiguration? Configuration { get; private set; }

    public AppInitSettings(DefaultSettings? settings = null)
    {
        Settings = settings ?? new DefaultSettings();

        _workingDirectory = DetermineWorkingDirectory();
        var settingsPath = CreateDirectory(_workingDirectory, Settings.SettingsFolderName);
        var storePath = CreateDirectory(_workingDirectory, Settings.StoreFolderName);
        _settingsFilePath = Path.Combine(settingsPath, Settings.SettingsFileName);
        _storeFilePath = Path.Combine(storePath, Settings.StoreFileName);
        Console.WriteLine("Setting file: {0}", _settingsFilePath);
        Console.WriteLine("Store file:   {0}", _storeFilePath);
        if (File.Exists(_settingsFilePath))
        {
            Configuration = BuildConfigForAppSettingFile();
            ReadAppConfigFile(Configuration);
        }
        ReadEnvironmentVariables();
    }

    private LogLevel ParseLogginLevelString()
    {
        if (Enum.TryParse<LogLevel>(LoggingLevelString, true, out var logLevel))
        {
            return logLevel;
        }
        return LogLevel.Information;
    }

    private IConfiguration BuildConfigForAppSettingFile()
    {
        var configurationBuilder = new ConfigurationBuilder();
        return configurationBuilder
            .AddJsonFile(_settingsFilePath)
            .Build();
    }

    private static string DetermineWorkingDirectory()
    {
        var workingDirectory = Environment.GetEnvironmentVariable("WORKDIR", EnvironmentVariableTarget.Process);
        if (string.IsNullOrEmpty(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine("Enviroment variable 'WORKDIR' isn't set. Using: '{0}'", workingDirectory);
        }
        else if (!Directory.Exists(workingDirectory))
        {
            var errorMessage = $"Enviroment variable 'WORKDIR' doesn't define a valid directory: '{workingDirectory}'";
            throw new ApplicationSettingsException(errorMessage, errorMessage);
        }
        return workingDirectory;
    }

    private static string CreateDirectory(string workDir, string folderName)
    {
        var folderPath = folderName;
        if (workDir != "")
        {
            folderPath = Path.Combine(workDir, folderName);
        }
        if (!Directory.Exists(folderPath))
        {
            var info = Directory.CreateDirectory(folderPath);
            Console.WriteLine("Create directory. {0}", info.FullName);
        }
        return folderPath;
    }
}
