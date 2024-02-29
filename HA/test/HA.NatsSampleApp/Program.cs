// See https://aka.ms/new-console-template for more information
using HA;
using HA.Nats;
using HA.Service.Settings;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;


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

var logger = loggerFactory.CreateLogger("Application");
logger.LogInformation("start app");
logger.LogInformation("read settings");

var appSettings = new AppSettings(
    loggerFactory.CreateLogger<AppSettings>(),
    appInitSettings);
appSettings.CheckSettings();

var natsOpts = new NatsOpts
{
    Url = appSettings.Nats.Url,
    Name = string.IsNullOrEmpty(appSettings.Nats.ClientName) ? "NATS-Sample-App" : appSettings.Nats.ClientName,
    AuthOpts = new NatsAuthOpts { 
        Username = appSettings.Nats.User, 
        Password = appSettings.Nats.Password }
};

var streamName = appSettings.NatsStream.StreamName;
var subject = appSettings.NatsStream.Subject;
var maxAgeInDays = appSettings.NatsStream.maxAgeInDays;
var streamEnable = !string.IsNullOrEmpty(streamName) && !string.IsNullOrEmpty(subject) && maxAgeInDays > 0;
var topicPrefix = appSettings.NatsStream.SubjectPrefix;
if (streamEnable)
{
    var natsUtils = new NatsUtils(loggerFactory.CreateLogger("NatsUtils  "));
    var connection = await natsUtils.CreateConnectionAsync(natsOpts, 5, 5);
    var streamItems = await natsUtils.CreateStreamAsync(connection, streamName, subject, maxAgeInDays);
    if (streamItems.Context == null)
        throw new ArgumentNullException("No nats context available");
    var isAvailable = await natsUtils.CheckStreamExistAsync(streamItems.Context, streamName);
    if (!isAvailable)
        throw new Exception($"Stream '{streamName}' is not available.");
}
var publisher = new NatsResilientPublisher(loggerFactory.CreateLogger("Publisher  "), natsOpts, streamEnable);
var count = 0;
while (true)
{
    var measurmentGood = new Measurement(DateTime.Now) {
        Device = "NatSampleApp",
        Quality = QualityInfos.Good,

    };
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatus", "OK"));
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatusAsBool", true));
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatusAsInt", 1));
    measurmentGood.Values.Add(MeasuredValue.Create("count", count++));
    logger.LogInformation("publish: {0}", measurmentGood.ToLineProtocol(TimeResolution.s));
    publisher.Publish($"{topicPrefix}.{measurmentGood.Device}", measurmentGood);
    Thread.Sleep(1000);

    var measurmentBad = new Measurement(DateTime.Now)
    {
        Device = "NatSampleApp",
        Quality = QualityInfos.Bad

    };
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatus", "DOWN"));
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatusAsBool", false));
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatusAsInt", 0));
    measurmentBad.Values.Add(MeasuredValue.Create("count", count++));
    logger.LogInformation("publish: {0}", measurmentBad.ToLineProtocol(TimeResolution.s));
    publisher.Publish($"{topicPrefix}.{measurmentBad.Device}", measurmentBad);
    Thread.Sleep(10000);
}

