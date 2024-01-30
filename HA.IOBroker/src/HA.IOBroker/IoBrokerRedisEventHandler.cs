using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HA.IOBroker.config;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HA.IOBroker;

public class IoBrokerRedisEventHandler
{
    private readonly DateTime m_UnixStartDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly CancellationTokenSource m_TokenSource = new CancellationTokenSource();
    private readonly List<DeviceBinding> m_DeviceBindings;
    private readonly ConcurrentQueue<IoBrokerValue> m_Queue = new ConcurrentQueue<IoBrokerValue>();
    private readonly IoBrokerCache m_IoBrokerCache;
    private readonly Task m_Task;

    public IoBrokerRedisEventHandler(List<DeviceBinding> deviceBindings)//, InfluxDbStore influxDbStore)
    {
        m_DeviceBindings = deviceBindings;
        m_TokenSource = new CancellationTokenSource();
        m_Task = Task.Run(DoWork, m_TokenSource.Token);
        m_IoBrokerCache = new IoBrokerCache(deviceBindings);//, influxDbStore);
    }

    public void OnEvent(RedisChannel channel, RedisValue message)
    {
        var channelFqn = (string)channel;
        var messageString = (string)message;
        foreach (var deviceBinding in m_DeviceBindings)
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
                TimeStamp = m_UnixStartDate.AddTicks(ioBrokerMessage.Ts * 10000),
                Value = ConvertValue(ioBrokerMessage.Value, dataType)
            };
            m_Queue.Enqueue(ioBrokerValue);
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
        finally
        {
        }
        return value;
    }

    public void Stop()
    {
        if (m_Task.Status == TaskStatus.Running)
        {
            m_TokenSource.Cancel();
        }
    }

    private void DoWork()
    {
        while (!m_TokenSource.Token.IsCancellationRequested)
        {
            Thread.Sleep(2000);
            var count = m_Queue.Count;
            var minTimeStamp = DateTime.MaxValue;
            if (count == 0)
            {
                continue;
            }
            var maxDateTime = DateTime.UtcNow.AddMilliseconds(-250);
            for (var i = 0; i < count; i++)
            {
                if (m_Queue.TryPeek(out var ioBrokerValue))
                {
                    minTimeStamp = ioBrokerValue.TimeStamp < minTimeStamp ? ioBrokerValue.TimeStamp : minTimeStamp;
                    if (ioBrokerValue.TimeStamp > maxDateTime)
                    {
                        break;
                    }
                    m_IoBrokerCache.AddValue(ioBrokerValue);
                    m_Queue.TryDequeue(out ioBrokerValue);
                }
                else
                {
                    break;
                }
            }
            m_IoBrokerCache.ProcessValues();
        }
    }
}