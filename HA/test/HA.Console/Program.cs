// See https://aka.ms/new-console-template for more information
using HA.Mqtt;
using HA.Nats;
using HA.Service;
using HA.Service.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using NATS.Client.Core;
using System.Collections.Concurrent;

namespace HA.ConsoleApp;

public class Program
{
    private static ILogger? _logger;

    public static void Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
            var appCommonSettings = new AppInitSettings();
            var loggerFactory = LoggerFactory.Create(builder => builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("HA", appCommonSettings.LoggingLevel)
                .SetMinimumLevel(appCommonSettings.LoggingLevel)
                .AddSimpleConsole(options => {
                    options.SingleLine = true;
                    options.TimestampFormat = "dd.MM HH:mm:ss ";
                    options.IncludeScopes = false;
                }));
            _logger = loggerFactory.CreateLogger<Program>();
            _logger.LogInformation("start ConsoleApp");

            /*Console.WriteLine($"[TID:{Thread.CurrentThread.ManagedThreadId}] Main");
            var testNoResult = NoResult("NoResult");
            var testNoResult2 = NoResult2("NoResult2");
            Task.WaitAll(testNoResult, testNoResult2);

            var test = GetString("a");
            var result = test.Result;
            System.Console.WriteLine(result);*/

            var simulator = new SimulatorObservableWorker(loggerFactory.CreateLogger<SimulatorObservableWorker>());
            simulator.SleepTimeMs = 3000;

            var url = "nats://192.168.111.47:4222,nats://192.168.111.49:4222,nats://192.168.111.95:4222";
            var natsOpts = new NatsOpts
            {
                Url = url,
                Name = "NATS-Test-Producer",
                AuthOpts = new NatsAuthOpts { Username = "nats", Password = "transfer" }
            };
            var natsPublisher = new NatsPublisher(loggerFactory.CreateLogger<NatsPublisher>(), natsOpts);
            var natsWorker = new NatsWorker(loggerFactory.CreateLogger<NatsWorker>(), natsPublisher);
            var natsTask = natsWorker.StartAsync(CancellationToken.None);

            var mqttPublisher = new MqttPublisher(loggerFactory.CreateLogger<MqttPublisher>(),
                "192.168.111.50", 1883, "Client1");
            var mqttWorker = new MqttWorker(loggerFactory.CreateLogger<MqttWorker>(), mqttPublisher);
            mqttWorker.PublishJson = true;
            mqttWorker.PublishLineProtocol = true;
            var mqttTask = mqttWorker.StartAsync(CancellationToken.None);

            var mqttObserver = new MeasurementObserver(loggerFactory.CreateLogger<Measurement>(), mqttWorker);
            var natsObserver = new MeasurementObserver(loggerFactory.CreateLogger<Measurement>(), natsWorker);

            mqttObserver.Subscribe(simulator.MeasurementObservable);
            natsObserver.Subscribe(simulator.MeasurementObservable);

            //var measurementObserver2 = new MeasurementObserver(loggerFactory.CreateLogger<Measurement>(), worker);
            //measurementObserver1.Subscribe(simulator.MeasurementObservable);

            var simulatorTask = simulator.StartAsync(CancellationToken.None);

            /*var machineName = Environment.MachineName;
            var osVersion = Environment.OSVersion.ToString();
            var version = Environment.Version.ToString();
            var counter = 0;
            while (true)
            {
                var measurment = new Measurement()
                {
                    Device = machineName,
                    Quality = QualityInfos.Good,
                };
                measurment.Tags.Add("MachineName", machineName);
                measurment.Tags.Add("OSVersion", osVersion);
                measurment.Tags.Add("Version", version);
                measurment.Values.Add(MeasuredValue.Create("UpTimeHour", TimeSpan.FromMilliseconds(Environment.TickCount64).TotalHours));
                measurment.Values.Add(MeasuredValue.Create("Counter", counter++));
                worker.ProcessMeasurement(measurment);
                _logger.LogInformation($"[TID:{Thread.CurrentThread.ManagedThreadId}]");
                Thread.Sleep(1000);
            }*/
            
            
            
            //task.Wait();

            Console.ReadLine();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static async Task<string> GetString(string a)
    {
        Console.WriteLine($"[TID:{Thread.CurrentThread.ManagedThreadId}] GetString");
        await Task.Delay(1000);
        var result = $"[TID:{Thread.CurrentThread.ManagedThreadId}] Test {a}";
        Console.WriteLine(result);
        return result;
    }

    public static async Task NoResult(string a)
    {
        Console.WriteLine($"[TID:{Thread.CurrentThread.ManagedThreadId}] NoResult");
        await Task.Delay(1200);
        Console.WriteLine($"[TID:{Thread.CurrentThread.ManagedThreadId}] NoResult-1200");
        //throw new Exception("Test");
        var result = $"[TID:{Thread.CurrentThread.ManagedThreadId}] Test {a}";
        Console.WriteLine(result);
    }

    public static async Task NoResult2(string a)
    {
        Console.WriteLine($"[TID:{Thread.CurrentThread.ManagedThreadId}] NoResult2");
        await Task.Delay(1000);
        Console.WriteLine($"[TID:{Thread.CurrentThread.ManagedThreadId}] NoResult2-1000");
        //throw new Exception("Test");
        var result = $"[TID:{Thread.CurrentThread.ManagedThreadId}] Test {a}";
        Console.WriteLine(result);
    }

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _logger?.LogCritical(ex, ex?.Message);
    }
}





