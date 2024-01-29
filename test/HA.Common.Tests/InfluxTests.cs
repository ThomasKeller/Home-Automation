using HA.Influx;
using HA.Store;
using Microsoft.Extensions.Logging;
using Moq;

namespace HA.Common.Tests
{
    public class InfluxTests
    {
        private static string _token = "22tNRGhED2ygMzXbWK9NpTErI1YMhJ2xKu5iRo3QXWG2oDrBl5S629ACV813zsvzymhQmKrFISU57N39oWdXuA==";
        private IInfluxStore _client = new InfluxSimpleStore("http://192.168.111.17:8086", "Test", "Keller", _token) { Timeout = 2000 };
        private ILoggerFactory _loggerFactory;

        [SetUp]
        public void Setup()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("ha", LogLevel.Debug)
                    .AddDebug()
                    .AddConsole());
        }

        [TearDown]
        public void Teardown()
        {
            _loggerFactory.Dispose();
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_throws_UnauthorizedException_when_using_invalid_token()
        {
            var sut = new InfluxSimpleStore("http://192.168.111.17:8086", "Test", "Keller", _token + "-");
            var reachable = sut.Ping();
            Assert.That(reachable, Is.True);
            Assert.Throws<InfluxSimpleStore.UnauthorizedException>(() => sut.WriteMeasurement(CreateMeasurement()));
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_client_throws_ArgumentNullException_when_using_empty_measurement()
        {
            var reachable = _client.Ping();
            Assert.That(reachable, Is.True);
            Assert.Throws<InfluxSimpleStore.BadRequestException>(() => _client.WriteMeasurement(new Measurement()));
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_client_throws_BadRequestException_when_using_measuremen_only_with_device_name()
        {
            var reachable = _client.Ping();
            Assert.That(reachable, Is.True);
            Assert.Throws<InfluxSimpleStore.BadRequestException>(() => _client.WriteMeasurement(new Measurement { Device = "Test" }));
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_client_throws_RestApiException_when_influx_server_is_not_reachable()
        {
            var sut = new InfluxSimpleStore("http://192.168.112.17:8086", "Test", "Keller", _token) { Timeout = 2000 };
            Assert.Throws<InfluxSimpleStore.RestApiException>(() => sut.WriteMeasurement(CreateMeasurement()));
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_is_reachable()
        {
            var pingResult = _client.Ping();
            Assert.That(pingResult, Is.True);
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_is_healthy()
        {
            var pingResult = _client.CheckHealth();
            Assert.That(pingResult, Is.True);
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_is_able_to_write_measurement()
        {
            Measurement measurement = CreateMeasurement();
            _client.WriteMeasurement(measurement);
            Assert.Pass();
            //Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            //Assert.That(result.IsSuccessful, Is.True);
        }

        private static Measurement CreateMeasurement(double dValue = 1.2345, int iValue = 1)
        {
            var measurement = new Measurement();
            measurement.Quality = QualityInfos.Good;
            measurement.Device = "UnitTest";
            measurement.Tags.Add("Test", "UnitTest");
            measurement.Values.Add(new MeasuredValue { Name = "VINT", Value = iValue });
            measurement.Values.Add(new MeasuredValue { Name = "VDOUBLE", Value = dValue });
            return measurement;
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_is_able_to_write_measurements()
        {
            _client.WriteMeasurements(new[] {
                CreateMeasurement(),
                CreateMeasurement(2.3456, 2)
            });
            Assert.Pass();
        }

        [Test]
        [Category("Integration")]
        public void check_that_influx_is_able_to_write_measurements2()
        {
            var measurement1 = CreateMeasurement();
            var measurement2 = CreateMeasurement(2.3456, 2);

            var measurements = new List<Measurement>();
            var store = new MeasurementStore();
            var client = new InfluxResilientStore(_loggerFactory.CreateLogger<InfluxResilientStore>(), _client, store);
            client.WriteMeasurements(new[] { measurement1, measurement2 });
            Thread.Sleep(500);
            client.WriteMeasurements(new[] { measurement1, measurement2 });
            Thread.Sleep(500);
            client.WriteMeasurements(new[] { measurement1, measurement2 });
            Thread.Sleep(2000);
        }

        [Test]
        public void check_that_influx_is_able_to_write_measurements_by_mocks()
        {
            var measurement1 = CreateMeasurement();
            var measurement2 = CreateMeasurement(2.3456, 2);
            var isStoreCalled = 0;
            var writeMeasurement1 = 0;
            var writeMeasurement2 = 0;

            var storeMock = new Mock<IMeasurementStore>();
            storeMock.Setup(x => x.Save(It.IsAny<Measurement>()))
                .Callback(() => isStoreCalled++);
            storeMock.Setup(x => x.Save(It.IsAny<IEnumerable<Measurement>>()))
                .Callback(() => isStoreCalled++);

            var influxSimpleStoreMock = new Mock<IInfluxStore>();
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(It.IsAny<Measurement>()))
                .Callback(() => writeMeasurement1++);
            influxSimpleStoreMock.Setup(x => x.WriteMeasurements(It.IsAny<IEnumerable<Measurement>>()))
                .Callback(() => writeMeasurement2++);

            var measurements = new List<Measurement>();
            var client = new InfluxResilientStore(_loggerFactory.CreateLogger<InfluxResilientStore>(),
                influxSimpleStoreMock.Object, storeMock.Object);
            client.WriteMeasurements(new[] { measurement1, measurement2 });
            Thread.Sleep(1000);

            storeMock.Verify(x => x.Save(It.IsAny<Measurement>()), Times.Never);
            storeMock.Verify(x => x.Save(It.IsAny<IEnumerable<Measurement>>()), Times.Never);
            influxSimpleStoreMock.Verify(x => x.WriteMeasurements(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
        }

        [Test]
        public void check_that_influx_is_able_to_write_measurements_by_mocks2()
        {
            var measurement1 = CreateMeasurement();
            var measurement2 = CreateMeasurement(2.3456, 2);
            var measurements = new[] { measurement1, measurement2 };
            var isStoreCalled = 0;
            var writeMeasurement1 = 0;
            var writeMeasurement2 = 0;

            var storeMock = new Mock<IMeasurementStore>();
            storeMock.Setup(x => x.Save(It.IsAny<Measurement>()))
                .Callback(() => isStoreCalled++);
            storeMock.Setup(x => x.Save(It.IsAny<IEnumerable<Measurement>>()))
                .Callback(() => isStoreCalled++);

            var influxSimpleStoreMock = new Mock<IInfluxStore>();
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(It.IsAny<Measurement>()))
                .Callback(() => writeMeasurement1++);
            //.Throws(new InfluxSimpleStore.BadRequestException(new RestSharp.RestResponse(), measurements));
            influxSimpleStoreMock.Setup(x => x.WriteMeasurements(measurements))
                .Callback(() => writeMeasurement2++)
                .Throws(new InfluxSimpleStore.BadRequestException(new RestSharp.RestResponse()));

            var client = new InfluxResilientStore(_loggerFactory.CreateLogger<InfluxResilientStore>(),
                influxSimpleStoreMock.Object, storeMock.Object);
            client.WriteMeasurements(measurements);
            Thread.Sleep(2000);

            storeMock.Verify(x => x.Save(It.IsAny<Measurement>()), Times.Never);
            storeMock.Verify(x => x.Save(It.IsAny<IEnumerable<Measurement>>()), Times.Never);
            influxSimpleStoreMock.Verify(x => x.WriteMeasurements(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
            influxSimpleStoreMock.Verify(x => x.WriteMeasurement(It.IsAny<Measurement>()), Times.Exactly(2));
        }

        [Test]
        public void check_that_measurements_are_written_to_filesystem_when_connection_is_unauthorized()
        {
            var measurement1 = CreateMeasurement();
            var measurement2 = CreateMeasurement(2.3456, 2);
            var measurements = new[] { measurement1, measurement2 };
            var isStoreCalled = 0;

            var storeMock = new Mock<IMeasurementStore>();
            storeMock.Setup(x => x.Save(It.IsAny<Measurement>()))
                .Callback(() => isStoreCalled++);
            storeMock.Setup(x => x.Save(It.IsAny<IEnumerable<Measurement>>()))
                .Callback(() => isStoreCalled++);

            var influxSimpleStoreMock = new Mock<IInfluxStore>();
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(It.IsAny<Measurement>()));
            influxSimpleStoreMock.Setup(x => x.WriteMeasurements(measurements))
                .Throws(new InfluxSimpleStore.UnauthorizedException(new RestSharp.RestResponse()));

            var client = new InfluxResilientStore(_loggerFactory.CreateLogger<InfluxResilientStore>(),
                influxSimpleStoreMock.Object, storeMock.Object);
            client.WriteMeasurements(measurements);
            Thread.Sleep(2000);

            storeMock.Verify(x => x.Save(It.IsAny<Measurement>()), Times.Never);
            storeMock.Verify(x => x.Save(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
            influxSimpleStoreMock.Verify(x => x.WriteMeasurements(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
            influxSimpleStoreMock.Verify(x => x.WriteMeasurement(It.IsAny<Measurement>()), Times.Never);
        }

        [Test]
        public void check_that_all_valid_measurements_are_written_to_influx_and_invalid_ignored()
        {
            var measurement1 = CreateMeasurement();
            var measurement2 = CreateMeasurement(2.3456, 2);
            var measurements = new[] { measurement1, measurement2 };
            var isStoreCalled = 0;

            var storeMock = new Mock<IMeasurementStore>();
            storeMock.Setup(x => x.Save(It.IsAny<Measurement>()))
                .Callback(() => isStoreCalled++);
            storeMock.Setup(x => x.Save(It.IsAny<IEnumerable<Measurement>>()))
                .Callback(() => isStoreCalled++);

            var influxSimpleStoreMock = new Mock<IInfluxStore>();
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(measurement1))
                .Throws(new InfluxSimpleStore.BadRequestException(new RestSharp.RestResponse()));
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(measurement2));

            influxSimpleStoreMock.Setup(x => x.WriteMeasurements(measurements))
                .Throws(new InfluxSimpleStore.BadRequestException(new RestSharp.RestResponse()));
            var client = new InfluxResilientStore(_loggerFactory.CreateLogger<InfluxResilientStore>(),
                influxSimpleStoreMock.Object, storeMock.Object);
            client.WriteMeasurements(measurements);
            Thread.Sleep(2000);

            storeMock.Verify(x => x.Save(It.IsAny<Measurement>()), Times.Never);
            storeMock.Verify(x => x.Save(It.IsAny<IEnumerable<Measurement>>()), Times.Never);
            influxSimpleStoreMock.Verify(x => x.WriteMeasurements(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
            influxSimpleStoreMock.Verify(x => x.WriteMeasurement(It.IsAny<Measurement>()), Times.Exactly(2));
        }

        [Test]
        public void check_that_all_valid_measurements_are_written_to_influx_and_invalid_ignored2()
        {
            var measurement1 = CreateMeasurement();
            var measurement2 = CreateMeasurement(2.3456, 2);
            var measurements = new[] { measurement1, measurement2 };
            var isStoreCalled = 0;

            var storeMock = new Mock<IMeasurementStore>();
            storeMock.Setup(x => x.Save(It.IsAny<Measurement>()))
                .Callback(() => isStoreCalled++);
            storeMock.Setup(x => x.Save(It.IsAny<IEnumerable<Measurement>>()))
                .Callback(() => isStoreCalled++);

            var influxSimpleStoreMock = new Mock<IInfluxStore>();
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(measurement1))
                .Throws(new InfluxSimpleStore.BadRequestException(new RestSharp.RestResponse()));
            influxSimpleStoreMock.Setup(x => x.WriteMeasurement(measurement2));

            influxSimpleStoreMock.Setup(x => x.WriteMeasurements(measurements))
                .Throws(new InfluxSimpleStore.RestApiException(new RestSharp.RestResponse()));
            var client = new InfluxResilientStore(_loggerFactory.CreateLogger<InfluxResilientStore>(),
                influxSimpleStoreMock.Object, storeMock.Object);
            client.WriteMeasurements(measurements);
            Thread.Sleep(2000);

            storeMock.Verify(x => x.Save(It.IsAny<Measurement>()), Times.Never);
            storeMock.Verify(x => x.Save(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
            influxSimpleStoreMock.Verify(x => x.WriteMeasurements(It.IsAny<IEnumerable<Measurement>>()), Times.Exactly(1));
            influxSimpleStoreMock.Verify(x => x.WriteMeasurement(It.IsAny<Measurement>()), Times.Never);
        }
    }
}