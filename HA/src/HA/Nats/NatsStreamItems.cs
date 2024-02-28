using NATS.Client.Core;
using NATS.Client.JetStream;

namespace HA.Nats
{
    public class NatsStreamItems
    {
        public NatsStreamItems(NatsConnection? connection = null, NatsJSContext? context = null)
        {
            Connection = connection;
            Context = context;
        }

        public NatsConnection? Connection { get; set; }

        public NatsJSContext? Context { get; set; }

        public INatsJSStream? Stream { get; set; }

        public bool ConnectionExists => Connection != null;

        public bool ContextExists => Context != null;

        public bool StreamExists => Stream != null;

        public Nullable<int> Test { get; set; }
    }
}
