using StackExchange.Redis;
using System.Diagnostics;

namespace HA.Common.Tests
{
    public class Tests
    {
        private ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect("192.168.111.50");

        [SetUp]
        public void Setup()
        {
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _redis.Dispose();
        }

        [Test]
        public void check_that_redis_stores_key_value_pairs()
        {
            var db = _redis.GetDatabase();
            Assert.That(db, Is.Not.Null);

            var value = "Test";
            Assert.That(db.StringSet("mykey", value), Is.True);
            var readValue = db.StringGet("mykey").ToString();
            Assert.That(value, Is.EqualTo(readValue));
        }

        [Test]
        public void Test1()
        {
            var db = _redis.GetDatabase();
            Assert.That(db, Is.Not.Null);
            var streamName = "event_stream";
            var count = 0;
            var random = new Random();
            if (db.KeyExists(streamName))
            {
                Assert.IsTrue(db.KeyDelete(streamName));
            }

            // add a value to the stream
            var key = db.StreamAdd(streamName, CreateValues(++count, random.NextDouble() * 10));
            var valueCount = db.StreamLength(streamName);
            Assert.That(valueCount, Is.EqualTo(1));
            Assert.IsTrue(db.StreamCreateConsumerGroup(streamName, "group1"));
            Assert.IsTrue(db.StreamCreateConsumerGroup(streamName, "group2"));

            var streamInfo = db.StreamInfo(streamName);
            var groupInfo = db.StreamGroupInfo(streamName);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task<RedisValue>>();
            for (var x = 0; x < 1000000; x++)
            {
                var t = db.StreamAddAsync(streamName, CreateValues(++count, random.NextDouble() * 10));
                //key = db.StreamAdd(streamName, CreateValues(++count, random.NextDouble() * 10));
                tasks.Add(t);
            }
            Task.WaitAll(Task.WhenAll(tasks));

            //await Task.WhenAll(tasks);
            stopwatch.Stop();
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds / 1000.0} Seconds");
            for (var x = 0; x < 1000000; x++)
            {
                //key = db.StreamAdd(streamName, CreateValues(++count, random.NextDouble() * 10));
            }

            //Assert.That(db.StreamLength(streamName), Is.EqualTo(101));
            var msg1 = db.StreamReadGroup(streamName, "group1", "consumer1", "$", 10);
            var msg2 = db.StreamReadGroup(streamName, "group1", "consumer1", "$", 10);
            var msg3 = db.StreamReadGroup(streamName, "group1", "consumer1", "$", 10);
            var msg4 = db.StreamReadGroup(streamName, "group1", "consumer1", "$", 10);

            var msg2_1 = db.StreamReadGroup(streamName, "group2", "consumer2", "$", 10);
            var msg2_2 = db.StreamReadGroup(streamName, "group2", "consumer2", "$", 10);
            var msg2_3 = db.StreamReadGroup(streamName, "group2", "consumer2", "$", 10);
            var msg2_4 = db.StreamReadGroup(streamName, "group2", "consumer2", "$", 10);

            streamInfo = db.StreamInfo(streamName);
            groupInfo = db.StreamGroupInfo(streamName);

            /*if (db.KeyExists(streamName))
            {
                var streamInfo = db.StreamInfo(streamName);
                var groupInfo = db.StreamGroupInfo(streamName);
                //db.StreamPendingMessages("events_stream");
                //var pending = db.StreamPending("events_stream");
                //var pending = db.StreamPending(streamName, "default");
            }
            */

            db.KeyDelete(streamName);
        }

        private NameValueEntry[] CreateValues(int count, double temperatur)
        {
            return new NameValueEntry[]
            {
                new NameValueEntry("ticks", DateTime.UtcNow.Ticks),
                new NameValueEntry("count", count),
                new NameValueEntry("temp", temperatur)
            };
        }
    }
}