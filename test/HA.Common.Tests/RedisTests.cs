using HA.Redis;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace HA.Tests
{
    public class RedisTests
    {
        private ILoggerFactory _loggerFactory;
        private IFileStore _fileStore;

        [SetUp]
        public void Setup()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("ha", LogLevel.Debug)
                    .AddDebug()
                    .AddConsole());
            _fileStore = new FileStore(_loggerFactory.CreateLogger<FileStore>(), "Error");
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
            var sut = new RedisPersistenceClient("localhost");
            var success = sut.AddMeasurement(CreateMeasurement(1.234));
            var info = sut.GetMeasurmentStreamInfo();
            Assert.That(sut.ClientName, Is.Not.Null);
            Assert.That(success, Is.True);
        }

        [Test]
        [Category("Integration")]
        public void check_that_we_can_read_from_group()
        {
            var sut = new RedisPersistenceClient("localhost");
            var measurment = CreateMeasurement(1.2345);
            var success = sut.AddMeasurement(measurment);
            var measurememt2 = sut.ReadFromGroup(1);
            measurememt2 = sut.ReadFromGroup(1);

            Assert.That(sut.ClientName, Is.Not.Null);
            Assert.That(measurememt2, Is.Not.Null);

            var streamLength = sut.StreamLength();
            var streamLength2 = sut.StreamTrim(20);
            streamLength = sut.StreamLength();
        }

        [Test]
        [Category("Integration")]
        public void check_that_we_can_push_to_stream()
        {
            var streamName = "MyStream";

            var sut = new RedisPushToStreamClient(
                _loggerFactory.CreateLogger<RedisPushToStreamClient>(),
                _fileStore,
                "192.168.111.50")
            { StreamName = streamName };

            var measurment1 = CreateMeasurement(1.2345);
            var measurment2 = CreateMeasurement(2.2345);

            var deleted = sut.DeleteKey("MyStream");

            Assert.That(sut.StreamTrim(0), Is.EqualTo(0));
            Assert.That(sut.PushToStream(measurment1), Is.True);
            Assert.That(sut.StreamLength, Is.EqualTo(1));
            Assert.That(sut.PushToStream(measurment2), Is.True);
            Assert.That(sut.StreamLength, Is.EqualTo(2));

            var sut2 = new RedisPullFromStreamClient("192.168.111.50") { StreamName = streamName };

            var idsAndMeasurements = sut2.ReadMeasurementsFromStream(1).ToArray();
            Assert.That(idsAndMeasurements.Length, Is.EqualTo(1));
            var id = idsAndMeasurements.First().Id;
            Assert.That(id, Is.Not.Null);
            var measurement = idsAndMeasurements.First().Item;

            idsAndMeasurements = sut2.ReadMeasurementsFromStream(2, id).ToArray();
            Assert.That(idsAndMeasurements.Length, Is.EqualTo(1));
        }

        [Test]
        public void check_that_we_write_to_filestore_if_Redis_is_not_reachable_for_measurements()
        {
            var iFileStoreCalled = 0;
            var fileStoreMock = new Mock<IFileStore>();
            fileStoreMock.Setup(x => x.WriteToFile(It.IsAny<string>(), It.IsAny<bool>()))
                .Callback(() => iFileStoreCalled++);
            var sut = new RedisPushToStreamClient(
                _loggerFactory.CreateLogger<RedisPushToStreamClient>(),
                _fileStore,
                "192.168.111.1")
            { StreamName = "MyStream" };
            Assert.That(sut.PushToStream(new Measurement()), Is.False);
            Assert.That(iFileStoreCalled, Is.EqualTo(1));
            Assert.That(sut.PushToStream(new Measurement()), Is.False);
            Assert.That(iFileStoreCalled, Is.EqualTo(2));
        }

        [Test]
        public void check_that_we_write_to_filestore_if_Redis_is_not_reachable_for_strings()
        {
            var iFileStoreCalled = 0;
            var fileStoreMock = new Mock<IFileStore>();
            fileStoreMock.Setup(x => x.WriteToFile(It.IsAny<string>(), It.IsAny<bool>()))
                .Callback(() => iFileStoreCalled++);
            var sut = new RedisPushToStreamClient(
                _loggerFactory.CreateLogger<RedisPushToStreamClient>(),
                _fileStore,
                "192.168.111.1")
            { StreamName = "MyStream" };
            Assert.That(sut.PushToStream("string1"), Is.False);
            Assert.That(iFileStoreCalled, Is.EqualTo(1));
            Assert.That(sut.PushToStream("string2"), Is.False);
            Assert.That(iFileStoreCalled, Is.EqualTo(2));
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