namespace HA.Tests;

public class MeasurementTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void check_that_lineprotocol_is_create_with_utc_time_s()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        var dateTimeUtcOrg = dateTimeUtc;
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4567); // 456 탎
        var sut = new Measurement(dateTimeUtc)
        {
            Device = "Test",
            Quality = QualityInfos.Uncertain
        };
        sut.Tags.Add("t1", "tag1");
        sut.Tags.Add("t2", "tag2");
        sut.AddValue("v1", 123);
        sut.AddValue("v2", 123.456);
        sut.AddValue("v3", "v3");

        var lp_s = sut.ToLineProtocol(TimeResolution.s);
        var measurement = Measurement.FromLineProtocol(lp_s);
        Assert.That(measurement.Ticks, Is.EqualTo(dateTimeUtcOrg.Ticks));
        Assert.That(measurement.Quality, Is.EqualTo(QualityInfos.Uncertain));
        Assert.That(measurement.Tags.Count, Is.EqualTo(2));
        Assert.That(measurement.Tags.ContainsKey("t1"), Is.True);
        Assert.That(measurement.Tags.ContainsKey("t2"), Is.True);
        Assert.That(measurement.Values.Count, Is.EqualTo(3));
        Assert.That(measurement.Values.Any(p => p.Name == "v1"), Is.True);
        Assert.That(measurement.Values.Any(p => p.Name == "v2"), Is.True);
        Assert.That(measurement.Values.Any(p => p.Name == "v3"), Is.True);
    }

    [Test]
    public void check_that_lineprotokol_is_create_with_local_time_s()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        var dateTime = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Local);
        var dateTimeUtc = dateTime.ToUniversalTime();
        dateTime = dateTime.AddMilliseconds(123);
        dateTime = dateTime.AddTicks(4567); // 456 탎
        var sut = new Measurement(dateTime)
        {
            Device = "Test",
            Quality = QualityInfos.Uncertain
        };
        sut.Tags.Add("t1", "tag1");
        sut.Tags.Add("t2", "tag2");
        sut.AddValue("v1", 123);
        sut.AddValue("v2", 123.456);
        sut.AddValue("v3", "v3");

        var lp_s = sut.ToLineProtocol(TimeResolution.s);
        var measurement = Measurement.FromLineProtocol(lp_s);
        Assert.That(measurement.Ticks, Is.EqualTo(dateTimeUtc.Ticks));
        Assert.That(measurement.Quality, Is.EqualTo(QualityInfos.Uncertain));
        Assert.That(measurement.Tags.Count, Is.EqualTo(2));
        Assert.That(measurement.Values.Count, Is.EqualTo(3));
    }

    [Test]
    public void check_that_lineprotokol_is_create_with_utc_time_ms()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        var dateTimeUtcOrg = dateTimeUtc;
        dateTimeUtc = dateTimeUtc.AddTicks(4567); // 456 탎
        var sut = new Measurement(dateTimeUtc)
        {
            Device = "Test",
            Quality = QualityInfos.Uncertain
        };
        sut.Tags.Add("t1", "tag1");
        sut.Tags.Add("t2", "tag2");
        sut.AddValue("v1", 123);
        sut.AddValue("v2", 123.456);
        sut.AddValue("v3", "v3");

        var lp_ms = sut.ToLineProtocol(TimeResolution.ms);
        var measurement = Measurement.FromLineProtocol(lp_ms);
        Assert.That(measurement.Ticks, Is.EqualTo(dateTimeUtcOrg.Ticks));
        Assert.That(measurement.Quality, Is.EqualTo(QualityInfos.Uncertain));
        Assert.That(measurement.Tags.Count, Is.EqualTo(2));
        Assert.That(measurement.Values.Count, Is.EqualTo(3));
    }

    [Test]
    public void check_that_lineprotokol_is_create_with_utc_time_us()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4560); // 456 탎
        var dateTimeUtcOrg = dateTimeUtc;
        var sut = new Measurement(dateTimeUtc)
        {
            Device = "Test",
            Quality = QualityInfos.Uncertain
        };
        sut.Tags.Add("t1", "tag1");
        sut.Tags.Add("t2", "tag2");
        sut.AddValue("v1", 123);
        sut.AddValue("v2", 123.456);
        sut.AddValue("v3", "v3");

        var lp_us = sut.ToLineProtocol(TimeResolution.us);
        var measurement = Measurement.FromLineProtocol(lp_us);
        Assert.That(measurement.Ticks, Is.EqualTo(dateTimeUtcOrg.Ticks));
        Assert.That(measurement.Quality, Is.EqualTo(QualityInfos.Uncertain));
        Assert.That(measurement.Tags.Count, Is.EqualTo(2));
        Assert.That(measurement.Values.Count, Is.EqualTo(3));
    }

    [Test]
    public void check_that_lineprotokol_is_create_with_utc_time_ns()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4567); // 456 탎
        var dateTimeUtcOrg = dateTimeUtc;
        var sut = new Measurement(dateTimeUtc)
        {
            Device = "Test",
            Quality = QualityInfos.Uncertain
        };
        sut.Tags.Add("t1", "tag1");
        sut.Tags.Add("t2", "tag2");
        sut.AddValue("v1", 123);
        sut.AddValue("v2", 123.456);
        sut.AddValue("v3", "v3");

        var lp_ns = sut.ToLineProtocol(TimeResolution.ns);
        var measurement = Measurement.FromLineProtocol(lp_ns);
        Assert.That(measurement.Ticks, Is.EqualTo(dateTimeUtcOrg.Ticks));
        Assert.That(measurement.Quality, Is.EqualTo(QualityInfos.Uncertain));
        Assert.That(measurement.Tags.Count, Is.EqualTo(2));
        Assert.That(measurement.Values.Count, Is.EqualTo(3));
    }

    [Test]
    public void check_that_measurment_toJson_works_correctly()
    {
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4567); // 456 탎
        var dateTimeUtcOrg = dateTimeUtc;
        var sut = new Measurement(dateTimeUtc)
        {
            Device = "Test",
            Quality = QualityInfos.Uncertain
        };
        sut.Tags.Add("t1", "tag1");
        sut.Tags.Add("t2", "tag2");
        sut.AddValue("v1", 123);
        sut.AddValue("v2", 123.456);
        sut.AddValue("v3", "v3");
        var ticks = DateTime.UtcNow.Ticks;
        sut.AddValue("v4", ticks);

        var json = sut.ToJson();
        var measurement = Measurement.FromJson(json);
        Assert.That(measurement, Is.Not.Null);
        Assert.That(measurement.Ticks, Is.EqualTo(dateTimeUtcOrg.Ticks));
        Assert.That(measurement.Quality, Is.EqualTo(QualityInfos.Uncertain));
        Assert.That(measurement.Tags.Count, Is.EqualTo(2));
        Assert.That(measurement.Values.Count, Is.EqualTo(4));
        Assert.That(measurement.Values.First(m => m.Name == "v4").Value, Is.EqualTo(ticks));
    }
}