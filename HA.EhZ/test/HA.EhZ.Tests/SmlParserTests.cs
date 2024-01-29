using FluentAssertions;
using HA.EhZ.Observer;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace HA.EhZ.Tests;

public class SmlParserTests
{
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("ha", LogLevel.Debug)
                .AddConsole());
    }

    [Test]
    public void StreamTest()
    {
        var list = new List<EhZMeasurement>();

        using var fileStream = File.OpenRead("ComPortStream.udp");
        var countToRead = 20000;
        var array = new byte[countToRead];
        var count = fileStream.Read(array, 0, countToRead);
        count.Should().BeGreaterThan(0);
        array[0] = 0;
        var smlTelegram = new SmlParser();

        for (var x = 0; x < array.Length - 6; x += 6)
        {
            var smallArray = new byte[6];
            Array.Copy(array, x, smallArray, 0, 6);
            var measurement = smlTelegram.AddBytes(smallArray);
            if (measurement != null)
            {
                list.Add(measurement);
            }
        }
        list.Count.Should().BeGreaterThan(0);
    }

    [Test]
    public void EhZClientTest()
    {
        var server = new UdpServerObserver(_loggerFactory.CreateLogger<UdpServerObserver>(), 
            IPAddress.Broadcast.ToString(), 5555);

        var observable = new ObservableTest<byte[]>();
        observable.Value = new byte[] { 1, 2, 3, 4, 5, 6};
        observable.Subscribe(server);
        observable.Start();

        Thread.Sleep(10000);

        observable.Stop();

        /*var observerServerTest = new ObserverTest<byte[]>();
        observerServerTest


        server.OnNext()


        var client = new UdpClientObservable(_loggerFactory.CreateLogger<UdpClientObservable>(), 5555);
        var observerTest = new ObserverTest<byte[]>();
        client.Subscribe(observerTest);
        client.Start();
        Thread.Sleep(10000);
        client.Stop();
        Thread.Sleep(1000);*/
    }
}