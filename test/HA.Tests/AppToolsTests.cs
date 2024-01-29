using HA.AppTools;
using HA.Influx;
using HA.Tests.Utils;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;

namespace HA.Tests;

public class AppToolsTests
{
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("ha", LogLevel.Debug));
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
    }

    [Test]
    public void check_that_AppInitSettings_reports_non_existing_settings_file()
    {
        Environment.SetEnvironmentVariable("WORKDIR", null, EnvironmentVariableTarget.Process);

        var sut = new AppInitSettings();

        Assert.That(sut.Configuration, Is.Null);
        Assert.That(sut.SettingsFileExists, Is.False);

        // default settings
        Assert.That(sut.LoggingLevel, Is.EqualTo(LogLevel.Information));
    }

    [Test]
    public void check_that_AppInitSettings_uses_WORKDIR_environment_variable()
    {
        if (Directory.Exists("test"))
        {
            Directory.Delete("test", true);
        }
        var di = Directory.CreateDirectory("test");
        Environment.SetEnvironmentVariable("WORKDIR", di.FullName, EnvironmentVariableTarget.Process);

        var sut = new AppInitSettings();

        Assert.That(sut.Configuration, Is.Null);
        Assert.That(sut.SettingsFileExists, Is.False);

        // default settings
        var settingsPath = Path.Combine(di.FullName, sut.Settings.SettingsFolderName);

        Assert.That(sut.LoggingLevel, Is.EqualTo(LogLevel.Information));
        Assert.That(Directory.Exists(settingsPath), Is.True);
        Assert.That(Directory.Exists(Path.Combine(di.FullName, sut.Settings.StoreFolderName)), Is.True);

        // copy settings file
        File.Copy("appsettings.json", sut.SettingsFilePath);

        sut = new AppInitSettings();

        Assert.That(sut.LoggingLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(Directory.Exists(settingsPath), Is.True);
        Assert.That(Directory.Exists(Path.Combine(di.FullName, sut.Settings.StoreFolderName)), Is.True);


        Directory.Delete(settingsPath, true);
        Assert.That(Directory.Exists(settingsPath), Is.False);
    }

    [Test]
     public void check_that_AppInitSettings_uses_existing_appsettings_file_by_changing_default_folder_name()
    {
        var settings = new AppInitSettings.DefaultSettings { SettingsFolderName = "mySettings" };
        var sut = new AppInitSettings(settings);

        Assert.That(sut.Configuration, Is.Not.Null);
        Assert.That(sut.SettingsFileExists, Is.True);

        Assert.That(sut.LoggingLevel, Is.EqualTo(LogLevel.Warning));
    }



    [Test]
    public void check_environment_variables_are_assigned_to_properties_with_EnvParameterAttribute()
    {
        Environment.SetEnvironmentVariable("ENV_STRING", "ABC", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("ENV_INT", "1234", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("ENV_BOOL", "true", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("ENV_DOUBLE", "1.234", EnvironmentVariableTarget.Process);

        var appSettings = new AppSettings(_loggerFactory.CreateLogger<AppSettings>());
        Assert.That(appSettings.EnvString, Is.EqualTo("ABC"));
        Assert.That(appSettings.EnvInt32, Is.EqualTo(1234));
        Assert.That(appSettings.EnvBool, Is.EqualTo(true));
        Assert.That(appSettings.EnvDouble, Is.EqualTo(1.234));
    }

    [Test]
    public void check_environment_variables_are_partially_assigned_to_properties_with_EnvParameterAttribute()
    {
        var appSettings = new AppSettings(_loggerFactory.CreateLogger<AppSettings>());

        var configParameters = appSettings.GetConfigParamenters().ToList();

        var envVariables = appSettings.GetEnvironmentVariables().ToList();
        Assert.That(envVariables.Count, Is.EqualTo(5));
        var configParamenters = appSettings.GetConfigParamenters().ToList();
        Assert.That(configParamenters.Count, Is.EqualTo(3));
        appSettings.ReadConfigFile("appsettings.json");
        Assert.That(appSettings.EnvString, Is.EqualTo("ABC"));
    }

    [Test]
    public void check_that_influx_app_settings_read_correctly()
    {
        Environment.SetEnvironmentVariable("INFLUX_TOKEN", "token", EnvironmentVariableTarget.Process);
        
        var defaultSettings = new AppInitSettings.DefaultSettings { SettingsFolderName = "mySettings" };
        var appSettings = new AppInitSettings(defaultSettings);
        var sut = new InfluxSettings(appSettings.Configuration);
        Assert.That(sut.InfluxUrl, Is.EqualTo("http://192.168.111.237:8086/"));
        Assert.That(sut.InfluxOrg, Is.EqualTo("Keller"));
        Assert.That(sut.InfluxBucket, Is.EqualTo("ha"));
        Assert.That(sut.InfluxToken, Is.EqualTo("token"));
    }

    [Test]
    public void check_that_InfluxSettings_throws_exception_because_token_is_empty()
    {
        Environment.SetEnvironmentVariable("INFLUX_TOKEN", "", EnvironmentVariableTarget.Process);

        var defaultSettings = new AppInitSettings.DefaultSettings { SettingsFolderName = "mySettings" };
        var appSettings = new AppInitSettings(defaultSettings);
        var sut = new InfluxSettings(appSettings.Configuration);
        Assert.That(sut.InfluxUrl, Is.EqualTo("http://192.168.111.237:8086/"));
        Assert.That(sut.InfluxOrg, Is.EqualTo("Keller"));
        Assert.That(sut.InfluxBucket, Is.EqualTo("ha"));
        Assert.That(sut.InfluxToken, Is.Null);
        Assert.Throws<ArgumentException>(() => sut.CheckRequiredProperties());
    }

    [Test]
    public void chech_that_ValueChangeStatistics_count_correctly()
    {
        var value = new ValueWithStatistic<int>(0) { Duration = TimeSpan.FromSeconds(1) };
        var test = new List<int>();

        for(var x = 0; x < 500; x++)
        {
            value.Value = value.Value + 1;
            Thread.Sleep(10);
            test.Add(value.DurationCount);
        }
        Assert.True(test.Count > 0);
    }

}