using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HA.Nats
{
    public record NatsOptions
    {
        public NatsOptions(string url, string? clientName = null, string? user = null, string? password = null)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            ClientName = clientName ?? $"{Environment.MachineName}-{Assembly.GetExecutingAssembly().GetName().Name}";
        }


        public string Url { get; private set; }

        public string ClientName { get; private set; }

        public string? User { get; private set; }

        public string? Password { get; private set; }
    }
}
