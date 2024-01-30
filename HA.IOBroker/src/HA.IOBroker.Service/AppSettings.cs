namespace HA.IOBroker.Service;

public class AppSettings
{
    private const string c_DevicesFileName = "devices.json";
    private const string c_StoreFileName = "iobroker.db";
    private const string c_StoreFolderName = "store";
    private const string c_SettingsFileName = "appsettings.json";
    private const string c_SettingsFolderName = "settings";
    private readonly string _workDirectory;

    public string SettingsFilePath { get; set; } = Path.Combine(c_SettingsFolderName, c_SettingsFileName);

    public string DeviceConfigFilePath { get; set; } = Path.Combine(c_SettingsFolderName, c_DevicesFileName);

    public string MeasurmentStoreFilePath { get; set; } = Path.Combine(c_StoreFolderName, c_StoreFileName);

    public string RedisHost { get; set; } = "192.168.111.50";

    public string RedisPortString { get; set; } = "6379";

    public int RedisPort { get; set; } = 6379;

    public string InfluxUrl { get; set; } = "http://192.168.111.237:8086/";

    public string InfluxOrg { get; set; } = "Keller";

    public string InfluxBucket { get; set; } = "ha_test";

    public string? InfluxToken { get; set; }

    public string MqttHost { get; set; } = "localhost";

    public int MqttPort { get; set; } = 1883;

    public AppSettings(string workDir = "")
    {
        _workDirectory = workDir;
        var settingsPath = CreateDirectory(_workDirectory, c_SettingsFolderName);
        var storePath = CreateDirectory(_workDirectory, c_StoreFolderName);

        SettingsFilePath = Path.Combine(settingsPath, c_SettingsFileName);
        DeviceConfigFilePath = Path.Combine(settingsPath, c_DevicesFileName);
        MeasurmentStoreFilePath = Path.Combine(storePath, c_StoreFileName);
    }

    public void Read()
    {
        var envConfig = Environment.GetEnvironmentVariable("ENV_CONFIG", EnvironmentVariableTarget.Process)?.ToUpperInvariant();
        var appConfigFilePath = Path.Combine("settings", "appsettings.json");
        if (envConfig != null && (envConfig == "1" || envConfig == "TRUE"))
        {
            Console.WriteLine("Read configuration from environment variables.");
            ReadEnvironmentParameters();
        }
        else if (!File.Exists(appConfigFilePath))
        {
            var settings = ResourceSettings.ResourceManager.GetString("settings");
            File.WriteAllText(SettingsFilePath, settings);
            Console.WriteLine("Write configuration file '{0}'.", SettingsFilePath);
            var devices = ResourceSettings.ResourceManager.GetString("devices");
            File.WriteAllText(DeviceConfigFilePath, devices);
            Console.WriteLine("Write devices file '{0}'.", SettingsFilePath);
            throw new Exception("Please configure service");
        }
        else
        {
            Console.WriteLine("Read configuration from file: {0}", appConfigFilePath);
            ReadAppConfigFile(appConfigFilePath);
        }
        ConsolidateAppSettings();
    }
    private string CreateDirectory(string workDir, string folderName)
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

    private void ReadAppConfigFile(string appConfigFilePath)
    {
        var configurationBuilder = new ConfigurationBuilder();
        IConfiguration conf = configurationBuilder
            .AddJsonFile(appConfigFilePath)
            .AddEnvironmentVariables()
            .Build();

        var sectionKostalClient = conf.GetRequiredSection("redis");
        var redisHost = sectionKostalClient.GetValue<string>("host");
        if (redisHost != null)
            RedisHost = redisHost;
        var redisPortString = sectionKostalClient.GetValue<string>("port");
        if (redisPortString != null)
            RedisPortString = redisPortString;

        var sectionInflux = conf.GetRequiredSection("influxDB");
        var influxUrl = sectionInflux.GetValue<string>("url");
        if (influxUrl != null)
            InfluxUrl = influxUrl;
        var org = sectionInflux.GetValue<string>("org");
        if (org != null)
            InfluxOrg = org;
        var bucket = sectionInflux.GetValue<string>("bucket");
        if (bucket != null)
            InfluxBucket = bucket;
        var token = sectionInflux.GetValue<string>("token");
        if (token != null)
            InfluxToken = token;

        var sectionMqtt = conf.GetRequiredSection("mqtt");
        MqttHost = sectionMqtt.GetValue<string>("host") ?? "localhost";
        MqttPort = sectionMqtt.GetValue("port", 1883);
    }

    private void ConsolidateAppSettings()
    {
        if (int.TryParse(RedisPortString, out int value))
            RedisPort = value;
        Console.WriteLine("Device-Config: {0}", DeviceConfigFilePath);
        Console.WriteLine();
        Console.WriteLine("Redis-Host:    {0}", RedisHost);
        Console.WriteLine("Redis-Port:    {0}", RedisPort);
        Console.WriteLine();
        Console.WriteLine("Influx-URL:    {0}", InfluxUrl);
        Console.WriteLine("Influx-Org:    {0}", InfluxOrg);
        Console.WriteLine("Influx-Bucket: {0}", InfluxBucket);
        Console.WriteLine("Influx-Token:  {0}", InfluxToken != null);
        Console.WriteLine();
        Console.WriteLine("\"MQTT-Host:        {0}", MqttHost);
        Console.WriteLine("\"MQTT-Port:        {0}", MqttPort);
        if (InfluxToken == null)
        {
            Console.WriteLine("Please provider environmet variables or config file:");
            Console.WriteLine("REDIS_HOST      | http://192.168.111.50");
            Console.WriteLine("REDIS_PORT      | 6379");
            Console.WriteLine();
            Console.WriteLine("INFLUX_URL      | http://192.168.111.4");
            Console.WriteLine("INFLUX_ORG      | Keller");
            Console.WriteLine("INFLUX_BUCKET   | ha_test");
            Console.WriteLine("INFLUX_TOKEN    | token");
            Console.WriteLine();
            throw new ArgumentNullException("INFLUX_TOKEN");
        }
    }

    private void ReadEnvironmentParameters()
    {
        var environmentVariableTarget = EnvironmentVariableTarget.Process;
        var envDeviceConfigPath = Environment.GetEnvironmentVariable("DEVICE_CONFIG_FILEPATH", environmentVariableTarget);
        if (envDeviceConfigPath != null)
            DeviceConfigFilePath = envDeviceConfigPath;
        var envRedisHost = Environment.GetEnvironmentVariable("REDIS_HOST", environmentVariableTarget);
        if (envRedisHost != null)
            RedisHost = envRedisHost;
        var envRedisPort = Environment.GetEnvironmentVariable("REDIS_PORT", environmentVariableTarget);
        if (envRedisPort != null)
            RedisPortString = envRedisPort;
        var envInfluxUrl = Environment.GetEnvironmentVariable("INFLUX_URL", environmentVariableTarget);
        if (envInfluxUrl != null)
            InfluxUrl = envInfluxUrl;
        var envInfluxOrg = Environment.GetEnvironmentVariable("INFLUX_ORG", environmentVariableTarget);
        if (envInfluxOrg != null)
            InfluxOrg = envInfluxOrg;
        var envInfluxBucket = Environment.GetEnvironmentVariable("INFLUX_BUCKET", environmentVariableTarget);
        if (envInfluxBucket != null)
            InfluxBucket = envInfluxBucket;
        var envInfluxToken = Environment.GetEnvironmentVariable("INFLUX_TOKEN", environmentVariableTarget);
        if (envInfluxToken != null)
            InfluxToken = envInfluxToken;
        MqttHost = Environment.GetEnvironmentVariable("MQTT_HOST", environmentVariableTarget) ?? "192.168.111.50";
        var envMqttPort = Environment.GetEnvironmentVariable("MQTT_PORT", environmentVariableTarget) ?? "1883";
        if (int.TryParse(envMqttPort, out int value))
        {
            MqttPort = value;
        }
    }
}