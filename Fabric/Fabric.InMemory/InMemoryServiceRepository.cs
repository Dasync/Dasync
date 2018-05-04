using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.ServiceRegistry;

namespace Dasync.Fabric.InMemory
{
    public class InMemoryServiceRepository : IServiceDiscovery, IServicePublisher
    {
        private static readonly Dictionary<string, ServiceRegistrationInfo> _services
            = new Dictionary<string, ServiceRegistrationInfo>();

        public static void Clear()
        {
            _services.Clear();
        }

        public Task<IEnumerable<ServiceRegistrationInfo>> DiscoverAsync(CancellationToken ct)
        {
            var result = _services?.Values ?? Enumerable.Empty<ServiceRegistrationInfo>();
            return Task.FromResult(result);
        }

        public Task PublishAsync(IEnumerable<ServiceRegistrationInfo> services, CancellationToken ct)
        {
            foreach (var info in services)
            {
                _services.Remove(info.Name);
                _services.Add(info.Name, info);
            }

            return Task.FromResult(true);
        }
    }
}
