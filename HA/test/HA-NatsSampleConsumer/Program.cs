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
        options.TimestampFormat = "dd.MM HH:mm:ss ";
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
    Name = string.IsNullOrEmpty(appSettings.Nats.ClientName) ? "NATS-Sample-Consumer-App" : appSettings.Nats.ClientName,
    AuthOpts = new NatsAuthOpts
    {
        Username = appSettings.Nats.User,
        Password = appSettings.Nats.Password
    }
};
var consoleObserver = new ConsoleObserver();
var natsObservable = new NatsSubscriber(
    loggerFactory.CreateLogger<NatsSubscriber>(),
    new NatsSubscriber.Parameters(natsOpts, filteredSubject: appSettings.NatsConsumer.FilteredSubject)
    {
        StreamName = appSettings.NatsConsumer.StreamName,
        ConsumerName = appSettings.NatsConsumer.ConsumerName,
        QueueGroup = appSettings.NatsConsumer.QueueGroup,
    });
natsObservable.Subscribe(consoleObserver);
await natsObservable.SubscibeAsync(appSettings.NatsConsumer.FilteredSubject);


