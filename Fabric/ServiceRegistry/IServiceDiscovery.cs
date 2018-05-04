using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.ServiceRegistry
{
    public interface IServiceDiscovery
    {
        Task<IEnumerable<ServiceRegistrationInfo>> DiscoverAsync(CancellationToken ct);
    }
}
