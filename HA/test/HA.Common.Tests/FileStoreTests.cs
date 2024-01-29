using Microsoft.Extensions.Logging;

namespace HA.Common.Tests
{
    public class FileStoreTests
    {
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
        public void check_that_filestore_write_to_file()
        {
            var sut = new FileStore(_loggerFactory.CreateLogger<FileStore>(), "testfolder");

            sut.WriteToFile(DateTime.Now.Ticks.ToString());
            Thread.Sleep(100);
            sut.WriteToFile(DateTime.Now.Ticks.ToString());

            var fileStoreData = sut.ReadFirstFile();
            Assert.That(fileStoreData.FileCount > 0);
            Assert.That(fileStoreData.FileInfo, Is.Not.Null);
            FileInfo info = fileStoreData.FileInfo;
            Assert.That(info, Is.Not.Null);
            Assert.That(sut.MarkAsProcessed(info), Is.True);
        }
    }
}