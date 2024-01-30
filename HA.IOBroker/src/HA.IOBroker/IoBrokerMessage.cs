using System;
using Newtonsoft.Json;

namespace HA.IOBroker;

public class IoBrokerMessage
{
    [JsonProperty(PropertyName = "val")]
    public object Value { get; set; }

    [JsonProperty(PropertyName = "ack")]
    public bool Ack { get; set; }

    [JsonProperty(PropertyName = "ts")]
    public long Ts { get; set; }

    [JsonProperty(PropertyName = "q")]
    public int Quality { get; set; }

    [JsonProperty(PropertyName = "from")]
    public string Adapter { get; set; }

    [JsonProperty(PropertyName = "user")]
    public string User { get; set; }

    [JsonProperty(PropertyName = "Lc")]
    public long Lc { get; set; }

    [JsonIgnore()]
    public string Fqn { get; set; }

    [JsonIgnore()]
    public DateTime TimeStamp { get; set; }
}