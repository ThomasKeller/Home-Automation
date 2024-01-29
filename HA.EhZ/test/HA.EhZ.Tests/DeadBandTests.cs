using System;
using FluentAssertions;
using NUnit.Framework;

namespace HA.EhZ.Tests;

public class DeadBandTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void AddValue_should_return_null_for_the_first_value()
    {
        var deadBand = new DeadBand();

        var result = deadBand.AddValue(DateTime.Now, 1001);
        result.Should().BeNull();
    }

    [Test]
    public void AddValue_should_return_null_for_the_values_within_timedeadband()
    {
        var deadBand = new DeadBand();

        var startTime = DateTime.Now.AddSeconds(-15);
        var value = 1000;

        for (var x = 0; x < 13; x++)
        {
            var actualResult = deadBand.AddValue(startTime, value++);
            actualResult.Should().BeNull();
        }
    }

    [Test]
    public void AddValue_should_calculate_result_when_value_is_outside_the_timedeadband()
    {
        var deadBand = new DeadBand();

        var startTime = DateTime.Now.AddSeconds(-15);
        var value = 1000;

        for (var x = 0; x < 13; x++)
        {
            var actualResult = deadBand.AddValue(startTime, value++);
            actualResult.Should().BeNull();
        }
        var result = deadBand.AddValue(startTime.AddSeconds(15), value);

        result.Value.Should().Be(value);
        result.TimeStamp.Should().Be(startTime.AddSeconds(15));
        result.TimeDifference.Seconds.Should().Be(15);
        result.Difference.Should().Be(13);
        result.ValueCompressed.Should().Be(14);
        result.Power.Should().BeGreaterThan(0);
    }

    [Test]
    public void AddValue_should_calculate_result_when_value_is_outside_the_timedeadband_with_two_values()
    {
        var deadBand = new DeadBand();

        var startTime = DateTime.Now.AddSeconds(-15);
        var value = 1000;

        deadBand.AddValue(startTime, value);
        var result = deadBand.AddValue(startTime.AddSeconds(16), ++value);

        result.Value.Should().Be(value);
        result.TimeStamp.Should().Be(startTime.AddSeconds(16));
        result.TimeDifference.Seconds.Should().Be(16);
        result.Difference.Should().Be(1);
        result.ValueCompressed.Should().Be(2);
        result.Power.Should().BeGreaterThan(0);
    }

    [Test]
    public void AddValue_should_return_null_when_values_didnt_changed_NoDifferenceDeadBand_is_not_reached()
    {
        var deadBand = new DeadBand();
        var startTime = DateTime.Now.AddSeconds(-15);
        var value = 1000;

        deadBand.AddValue(startTime, value);
        // time dead band is reached, but values are equal and ValuesEqualDeadBand is not reached
        var result = deadBand.AddValue(startTime.AddSeconds(16), value);

        result.Should().Be(null);
    }

    [Test]
    public void AddValue_should_calculate_result_ValuesEqualDeadBand_is_reached()
    {
        var deadBand = new DeadBand();
        var startTime = DateTime.Now.AddMinutes(-10);
        var value = 1000;

        deadBand.AddValue(startTime, value);
        // time dead band is reached, but values are equal and ValuesEqualDeadBand is not reached
        var result = deadBand.AddValue(startTime.AddMinutes(10), value);

        result.Value.Should().Be(value);
        result.TimeStamp.Should().Be(startTime.AddMinutes(10));
        result.TimeDifference.TotalSeconds.Should().Be(10 * 60);
        result.Difference.Should().Be(0);
        result.ValueCompressed.Should().Be(2);
        result.Power.Should().Be(0);
    }

    [Test]
    public void AddValue_should_calculate_power()
    {
        var timeDeadBand = new DeadBand();
        var startTime = DateTime.Now.AddMinutes(-10);
        var value = 1000;

        timeDeadBand.AddValue(startTime, value);
        // time dead band is reached, but values are equal and ValuesEqualDeadBand is not reached
        var result = timeDeadBand.AddValue(startTime.AddMinutes(10), value);

        result.Value.Should().Be(value);
        result.TimeStamp.Should().Be(startTime.AddMinutes(10));
        result.TimeDifference.TotalSeconds.Should().Be(10 * 60);
        result.Difference.Should().Be(0);
        result.ValueCompressed.Should().Be(2);
        result.Power.Should().Be(0);
    }
}