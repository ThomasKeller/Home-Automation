using HA.Common;
using HA.Common.Observable;
using HA.IOBroker.config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HA.IOBroker;

public class IoBrokerRedisObservable : ObservableBase<Measurement>
{
    private readonly ILogger _logger;

    private readonly DateTime _unixStartDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
    private readonly List<DeviceBinding> _deviceBindings;
    private readonly ConcurrentQueue<IoBrokerValue> _queue = new ConcurrentQueue<IoBrokerValue>();
    private readonly IoBrokerCache _ioBrokerCache;
    private readonly Task _task;

    public IoBrokerRedisObservable(ILogger logger, List<DeviceBinding> deviceBindings)
    {
        _logger = logger;
        _deviceBindings = deviceBindings;
        _task = Task.Run(DoWork, _tokenSource.Token);
        _ioBrokerCache = new IoBrokerCache(deviceBindings);
    }

    public int EventQueueCount => _queue.Count;

    public DateTime LastMeasurementSentAt { get; private set; } = DateTime.MinValue;

    public void OnEvent(RedisChannel channel, RedisValue message)
    {
        var channelFqn = (string)channel;
        var messageString = (string)message;
        foreach (var deviceBinding in _deviceBindings)
        {
            if (!channelFqn.Contains(deviceBinding.DeviceId))
                continue;
            var propertyKey = deviceBinding.Properties.Keys.FirstOrDefault(v =>
                channelFqn.EndsWith(v, StringComparison.OrdinalIgnoreCase));
            var propertyName = string.Empty;
            var dataType = FieldType.Unknown;
            if (propertyKey != null)
            {
                propertyName = deviceBinding.Properties[propertyKey].PropertyName;
                dataType = deviceBinding.Properties[propertyKey].GetFieldType();
            }
            else
            {
                var pos = channelFqn.LastIndexOf(".") + 1;
                propertyName = channelFqn.Substring(pos);
            }
            var ioBrokerMessage = JsonConvert.DeserializeObject<IoBrokerMessage>(messageString);
            var ioBrokerValue = new IoBrokerValue
            {
                Channel = channelFqn,
                DeviceId = deviceBinding.DeviceId,
                DeviceName = deviceBinding.DeviceName.Replace(" ", "_"),
                PropertyName = propertyName,
                GroupName = deviceBinding.GroupName,
                TimeStamp = _unixStartDate.AddTicks(ioBrokerMessage.Ts * 10000),
                Value = ConvertValue(ioBrokerMessage.Value, dataType)
            };
            _queue.Enqueue(ioBrokerValue);
        }
    }

    private object ConvertValue(object value, FieldType fieldType)
    {
        try
        {
            switch (fieldType)
            {
                case FieldType.Unknown:
                    return value;

                case FieldType.Float:
                    return Convert.ToDouble(value);

                case FieldType.Integer:
                    return Convert.ToInt64(value);

                case FieldType.String:
                    return Convert.ToString(value);

                case FieldType.Boolean:
                    return Convert.ToBoolean(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return value;
    }

    public void Stop()
    {
        if (_task.Status == TaskStatus.Running)
        {
            _tokenSource.Cancel();
        }
    }

    private void DoWork()
    {
        while (!_tokenSource.Token.IsCancellationRequested)
        {
            Thread.Sleep(500);
            var count = _queue.Count;
            var minTimeStamp = DateTime.MaxValue;
            if (count == 0)
            {
                continue;
            }
            var maxDateTime = DateTime.UtcNow.AddMilliseconds(-250);
            for (var i = 0; i < count; i++)
            {
                if (_queue.TryPeek(out var ioBrokerValue))
                {
                    minTimeStamp = ioBrokerValue.TimeStamp < minTimeStamp ? ioBrokerValue.TimeStamp : minTimeStamp;
                    if (ioBrokerValue.TimeStamp > maxDateTime)
                    {
                        break;
                    }
                    _ioBrokerCache.AddValue(ioBrokerValue);
                    _queue.TryDequeue(out ioBrokerValue);
                }
                else
                {
                    break;
                }
            }
            var measurements = _ioBrokerCache.ProcessValues();
            foreach (var measurement in measurements)
            {
                LastMeasurementSentAt = DateTime.Now;
                ExecuteOnNext(measurement);
            }
        }
    }
}