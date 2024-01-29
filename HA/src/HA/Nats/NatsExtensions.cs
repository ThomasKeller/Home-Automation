using NATS.Client.JetStream;

namespace HA.Nats;

public static class NatsExtensions
{
    public static async Task<bool> CheckStreamExistAsync(this NatsJSContext jsContext, string streamName)
    {
        await foreach (var name in jsContext.ListStreamNamesAsync())
        {
            if (name.Equals(streamName, StringComparison.InvariantCulture))
                return true;
        }
        return false;
    }
}
