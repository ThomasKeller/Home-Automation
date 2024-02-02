// See https://aka.ms/new-console-template for more information
using HA;
using HA.Nats;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

Console.WriteLine("Hello, World!");

var loggerFactory = LoggerFactory.Create(builder => builder
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning)
    .AddFilter("HA", LogLevel.Debug)
    .SetMinimumLevel(LogLevel.Information)
    .AddSimpleConsole(options => {
        options.SingleLine = true;
        options.TimestampFormat = "yy-MM-dd HH:mm:ss.fff ";
    }));
var logger = loggerFactory.CreateLogger("app");
logger.LogInformation("start app");

var url = "192.168.111.49:4222";
var natsOpts = new NatsOpts
{
    Url = url,
    Name = "NATS-Test-Producer",
    AuthOpts = new NatsAuthOpts { Username = "nats", Password = "transfer" }
};
var sut = new NatsPublisher(loggerFactory.CreateLogger<NatsPublisher>(), natsOpts);

while (true)
{
    var measurmentGood = new Measurement(DateTime.Now) {
        Device = "NatSampleApp",
        Quality = QualityInfos.Good,

    };
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatus", "OK"));
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatusAsBool", true));
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatusAsInt", 1));
    measurmentGood.Values.Add(MeasuredValue.Create("healthStatusAsDouble", 1.0));
    logger.LogInformation("publish: {0}", measurmentGood.ToLineProtocol(TimeResolution.s));
    await sut.PublishAsync($"health.test.{measurmentGood.Device}", measurmentGood.ToLineProtocol());
    Thread.Sleep(1000);

    var measurmentBad = new Measurement(DateTime.Now)
    {
        Device = "NatSampleApp",
        Quality = QualityInfos.Bad

    };
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatus", "DOWN"));
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatusAsBool", false));
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatusAsInt", 0));
    measurmentBad.Values.Add(MeasuredValue.Create("healthStatusAsDouble", 0.0));
    logger.LogInformation("publish: {0}", measurmentBad.ToLineProtocol(TimeResolution.s));
    await sut.PublishAsync($"health.test.{measurmentBad.Device}", measurmentBad.ToLineProtocol());
    Thread.Sleep(1000);

}
