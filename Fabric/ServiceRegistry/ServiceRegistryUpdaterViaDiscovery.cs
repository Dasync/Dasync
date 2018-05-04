using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.ServiceRegistry
{
    public interface IServiceRegistryUpdaterViaDiscovery
    {
        Task UpdateAsync(CancellationToken ct);
    }

    public class ServiceRegistryUpdaterViaDiscovery : IServiceRegistryUpdaterViaDiscovery
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly IServiceDiscovery[] _serviceDiscoveries;

        public ServiceRegistryUpdaterViaDiscovery(
            IServiceRegistry serviceRegistry,
            IServiceDiscovery[] serviceDiscoveries)
        {
            _serviceRegistry = serviceRegistry;
            _serviceDiscoveries = serviceDiscoveries;
        }

        public async Task UpdateAsync(CancellationToken ct)
        {
            foreach (var serviceDiscovery in _serviceDiscoveries)
            {
                var services = await serviceDiscovery.DiscoverAsync(ct);
                foreach (var info in services)
                {
                    var infoCopy = info;

                    var existingServiceRegistration = _serviceRegistry.AllRegistrations.SingleOrDefault(
                        i => i.ServiceName == (info.Name ?? info.QualifiedServiceTypeName));
                    if (existingServiceRegistration != null)
                    {
                        infoCopy.QualifiedServiceTypeName =
                            existingServiceRegistration.ServiceType?.AssemblyQualifiedName;

                        infoCopy.QualifiedImplementationTypeName =
                            existingServiceRegistration.ImplementationType?.AssemblyQualifiedName;

                        infoCopy.IsExternal = existingServiceRegistration.IsExternal;
                    }

                    _serviceRegistry.Register(infoCopy);
                }
            }
        }
    }
}
