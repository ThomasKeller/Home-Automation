namespace HA.Tests;

public class EpochExtensionTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void check_that_utc_datetime_can_convert_to_epoch_and_back_nano_seconds()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        //  Date and time (GMT): Thursday, December 7, 2023 17:34:46 
        //  Date and time (UTC): Thursday, December 7, 2023 16:34:46
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4567); // 456 탎
        var expectedEpoch_ns = 1701970486123456700;
        // one tick is 100 ns or 0.1 탎
        var epochUtc_ns = dateTimeUtc.ToEpoch();
        Assert.That(epochUtc_ns, Is.EqualTo(expectedEpoch_ns));
        
        var convertedDateTimeUtc = epochUtc_ns.FromEpoch(TimeResolution.ns);
        Assert.That(dateTimeUtc, Is.EqualTo(convertedDateTimeUtc));
    }

    [Test]
    public void check_that_utc_datetime_can_convert_to_epoch_and_back_micro_seconds()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        //  Date and time (GMT): Thursday, December 7, 2023 17:34:46 
        //  Date and time (UTC): Thursday, December 7, 2023 16:34:46
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4560);   // 456 탎
        var expectedEpoch_us = 1701970486123456;    
        // one tick is 100 ns or 0.1 탎
        var epochUtc_us = dateTimeUtc.ToEpoch(TimeResolution.us);
        Assert.That(epochUtc_us, Is.EqualTo(expectedEpoch_us));

        var convertedDateTimeUtc = epochUtc_us.FromEpoch(TimeResolution.us);
        Assert.That(dateTimeUtc, Is.EqualTo(convertedDateTimeUtc));
    }

    [Test]
    public void check_that_utc_datetime_can_convert_to_epoch_and_back_milli_seconds()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        //  Date and time (GMT): Thursday, December 7, 2023 17:34:46 
        //  Date and time (UTC): Thursday, December 7, 2023 16:34:46
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        var expectedEpoch_ms = 1701970486123;
        // one tick is 100 ns or 0.1 탎
        var epochUtc_ms = dateTimeUtc.ToEpoch(TimeResolution.ms);
        Assert.That(epochUtc_ms, Is.EqualTo(expectedEpoch_ms));

        var convertedDateTimeUtc = epochUtc_ms.FromEpoch(TimeResolution.ms);
        Assert.That(dateTimeUtc, Is.EqualTo(convertedDateTimeUtc));
    }

    [Test]
    public void check_that_utc_datetime_can_convert_to_epoch_and_back_seconds()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        //  Date and time (GMT): Thursday, December 7, 2023 17:34:46 
        //  Date and time (UTC): Thursday, December 7, 2023 16:34:46
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        var expectedEpoch_s = 1701970486;
        // one tick is 100 ns or 0.1 탎
        var epochUtc_s = dateTimeUtc.ToEpoch(TimeResolution.s);
        Assert.That(epochUtc_s, Is.EqualTo(expectedEpoch_s));

        var convertedDateTimeUtc = epochUtc_s.FromEpoch(TimeResolution.s);
        Assert.That(dateTimeUtc, Is.EqualTo(convertedDateTimeUtc));
    }

    [Test]
    public void check_that_utc_datetime_can_convert_to_a_epoch_string()
    {
        //  Epoch Timestamp [s]:  1701970486             (10^0)
        //  Epoch Timestamp [ms]: 1701970486 000         (10^-3)
        //  Epoch Timestamp [탎]: 1701970486 000 000     (10^-6)
        //  Epoch Timestamp [ns]: 1556813561 098 000 000 (10^-9)
        //  Date and time (GMT): Thursday, December 7, 2023 17:34:46 
        //  Date and time (UTC): Thursday, December 7, 2023 16:34:46
        var dateTimeUtc = new DateTime(2023, 12, 7, 17, 34, 46, DateTimeKind.Utc);
        dateTimeUtc = dateTimeUtc.AddMilliseconds(123);
        dateTimeUtc = dateTimeUtc.AddTicks(4567);   // 456 탎
        var expectedEpochString_s = "1701970486";
        var expectedEpochString_ms = "1701970486123";
        var expectedEpochString_us = "1701970486123456";
        var expectedEpochString_ns = "1701970486123456700";

        // one tick is 100 ns or 0.1 탎
        var epochUtcString_s = dateTimeUtc.ToEpochString(TimeResolution.s);
        var epochUtcString_ms = dateTimeUtc.ToEpochString(TimeResolution.ms);
        var epochUtcString_us = dateTimeUtc.ToEpochString(TimeResolution.us);
        var epochUtcString_ns = dateTimeUtc.ToEpochString(TimeResolution.ns);
        
        Assert.That(epochUtcString_s, Is.EqualTo(expectedEpochString_s));
        Assert.That(epochUtcString_ms, Is.EqualTo(expectedEpochString_ms));
        Assert.That(epochUtcString_us, Is.EqualTo(expectedEpochString_us));
        Assert.That(epochUtcString_ns, Is.EqualTo(expectedEpochString_ns));
    }

    

}