﻿using HA.AppTools;
using HA.Influx;
using HA.Nats;
using Microsoft.Extensions.Logging;

namespace HA.InfluxWriter.Service;

public class AppSettings
{
    public AppInitSettings Application { get; private set; }

    public InfluxSettings Influx { get; private set; }

    public NatsSettings Nats { get; private set; }  

    public AppSettings(ILogger logger, AppInitSettings appInitSettings)
    {
        Application = appInitSettings;
        Influx = new InfluxSettings(Application.Configuration);
        Nats = new NatsSettings(Application.Configuration);
    }

    public void CheckSettings()
    {
        Application.CheckSettings();
        Influx.CheckSettings();
        Nats.CheckSettings();
    }
}