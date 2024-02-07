using HA.Mqtt;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;

namespace HA.Tests
{
    public class MqttTests
    {
        private ILoggerFactory _loggerFactory;
        private IMqttPublisher _mqttPublisher;

        [SetUp]
        public void Setup()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("ha", LogLevel.Debug)
                    .AddDebug()
                    .AddConsole());
            _mqttPublisher = new MqttPublisher(_loggerFactory.CreateLogger<MqttPublisher>(), "localhost");
        }

        [TearDown]
        public void Teardown()
        {
            _loggerFactory.Dispose();
        }

        [Test]
        [Category("Integration")]
        public void check_that_we_create_a_unique_name()
        {
            var sut = new MqttPublisher(_loggerFactory.CreateLogger<MqttPublisher>(), "localhost");
            if (sut != null)
            {
                for (var x = 0; x < 10; x++)
                {
                    var task = sut.PublishAsync(CreateMeasurement(123 * x));
                    var success = task.Result;
                    AsyncHelper.RunSync(() => sut.PublishAsync(CreateMeasurement(123 * x)));
                    Thread.Sleep(1000);
                }
            }
            Assert.That(sut, Is.Not.Null);
        }

        private Measurement CreateMeasurement(double value)
        {
            var measurement = new Measurement
            {
                Device = "HA.Test",
                Quality = QualityInfos.Good
            };
            measurement.Tags.Add("T1", "Tag1");
            measurement.Tags.Add("T2", "Tag2");
            measurement.Values.Add(MeasuredValue.Create("V1", value));
            return measurement;
        }
    }
}