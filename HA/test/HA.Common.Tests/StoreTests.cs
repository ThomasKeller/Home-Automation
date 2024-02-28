using HA.Store;
using Microsoft.EntityFrameworkCore;

namespace HA.Tests
{
    public class StoreTests
    {
        [SetUp]
        public void Setup()
        {
            using (var db = new DatabaseContext())
            {
                db.Database.EnsureCreated();
            }
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void TestExchange()
        {
            using (var db = new DatabaseContext())
            {
                var deleteResult = db.MeasurementEntities.ExecuteDelete();
                var count = db.SaveChanges();

                var measurement = CreateMeasurement();
                var item = new MeasurementEntity()
                {
                    CreatedOn = DateTime.Now,
                    LineProtocol = measurement.ToJson()
                };
                var result = db.MeasurementEntities.Add(item);
                count = db.SaveChanges();
                Assert.That(count, Is.EqualTo(1));

                var items = db.MeasurementEntities.ToList();
                Assert.That(items.Count, Is.EqualTo(1));

                var storedItem = items.First();
                var removeResult = db.MeasurementEntities.Remove(storedItem);
                count = db.SaveChanges();

                items = db.MeasurementEntities.ToList();
                Assert.That(items.Count, Is.EqualTo(0));
            }
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
    }
}