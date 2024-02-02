// See https://aka.ms/new-console-template for more information
using HA;
using HA.AppTools;
using HA.Nats;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

var loggerFactory = LoggerFactory.Create(builder => builder
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning)
    .AddFilter("ha", LogLevel.Debug)
    .SetMinimumLevel(LogLevel.Warning)
    .AddSimpleConsole(options => {
        options.SingleLine = true;
        options.TimestampFormat = "yy-MM-dd HH:mm:ss.fff ";
    }));

var url = "192.168.111.49:4222";
var natsOpts = new NatsOpts
{
    Url = url,
    Name = "NATS-Test-Comsumer",
    AuthOpts = new NatsAuthOpts { Username = "nats", Password = "transfer" },
};

var consoleObserver = new ConsoleObserver();
var natsObservable = new NatsSubscriber(loggerFactory.CreateLogger<NatsSubscriber>(), natsOpts);


natsObservable.Subscribe(consoleObserver);

await natsObservable.SubscibeAsync("health.test.*");


