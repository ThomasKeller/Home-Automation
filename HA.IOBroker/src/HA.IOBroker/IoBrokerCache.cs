using HA;
using HA.IOBroker.config;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HA.IOBroker;

public class IoBrokerCache
{
    //private readonly ILogger _logger;
    private readonly List<DeviceBinding> _deviceBinding;
    private readonly List<string> _deviceIds;
    private readonly SortedDictionary<string, IoBrokerValue> _values = new SortedDictionary<string, IoBrokerValue>();
    private readonly ConcurrentBag<IoBrokerValue> _valuesToStore = new ConcurrentBag<IoBrokerValue>();

    public IoBrokerCache(/*ILogger logger,*/ List<DeviceBinding> deviceBindings)
    {
        _deviceBinding = deviceBindings;
        _deviceIds = _deviceBinding.SelectMany(db => db.GetDeviceIds()).ToList();
    }

    public void AddValue(IoBrokerValue ioBrokerValue)
    {
        if (_values.ContainsKey(ioBrokerValue.Channel))
        {
            var value = _values[ioBrokerValue.Channel];
            if (value.Value != ioBrokerValue.Value
                && value.TimeStamp != ioBrokerValue.TimeStamp)
            {
                _values[ioBrokerValue.Channel] = ioBrokerValue;
                _valuesToStore.Add(ioBrokerValue);
            }
        }
        else
        {
            _values.Add(ioBrokerValue.Channel, ioBrokerValue);
            _valuesToStore.Add(ioBrokerValue);
            //_logger.LogDebug("First value: {channel}, {time}, {value}",
            //s    ioBrokerValue.Channel, ioBrokerValue.TimeStamp, ioBrokerValue.Value);
        }
    }

    public List<Measurement> ProcessValues()
    {
        var ioBrokerValues = GetIoBrokerValues();
        var measurements = new List<Measurement>();
        foreach (var deviceIdWithValues in ioBrokerValues.Select(d => d.DeviceId).Distinct())
        {
            var deviceValues = ioBrokerValues.Where(d => d.DeviceId == deviceIdWithValues);
            Measurement measurement = null;
            foreach (var deviceValue in deviceValues)
            {
                var found = _deviceIds.Any(deviceId => deviceValue.Channel.EndsWith(deviceId));
                if (found)
                {
                    measurement = measurement == null
                        ? CreateMeasurement(deviceValue)
                        : AddValuesToMeasurement(measurement, deviceValue);
                }
            }
            if (measurement != null)
            {
                measurements.Add(measurement);
            }
        }
        return measurements;
    }

    private static Measurement CreateMeasurement(IoBrokerValue deviceValue)
    {
        var measurement = new Measurement
        {
            Device = deviceValue.GroupName,
            Quality = QualityInfos.Good,
            Ticks = deviceValue.TimeStamp.ToLocalTime().Ticks
        };

        measurement.Tags.Add("DeviceId", deviceValue.DeviceId);
        measurement.Tags.Add("DeviceName", deviceValue.DeviceName);
        return AddValuesToMeasurement(measurement, deviceValue);
    }

    private static Measurement AddValuesToMeasurement(Measurement measurement, IoBrokerValue deviceValue)
    {
        var pos = deviceValue.Channel.LastIndexOf(".") + 1;
        var name = deviceValue.Channel.Substring(pos);
        var first = measurement.Values.FirstOrDefault(mv => mv.Name == deviceValue.PropertyName);

        if (first != null)
        {
            first.Value = deviceValue.Value;
        }
        else
        {
            measurement.Values.Add(MeasuredValue.Create(deviceValue.PropertyName, deviceValue.Value));
        }
        return measurement;
    }

    private List<IoBrokerValue> GetIoBrokerValues()
    {
        var ioBrokerValues = new List<IoBrokerValue>();
        var count = _valuesToStore.Count;
        for (int i = 0; i < count; i++)
        {
            if (!_valuesToStore.TryTake(out var value))
            {
                break;
            }
            ioBrokerValues.Add((IoBrokerValue)value.Clone());
        }
        return ioBrokerValues;
    }
}