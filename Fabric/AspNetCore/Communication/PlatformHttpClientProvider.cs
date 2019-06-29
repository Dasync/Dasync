using System.Collections.Generic;
using System.Linq;
using Dasync.Modeling;
using Dasync.Serialization;

namespace Dasync.AspNetCore.Communication
{
    public interface IPlatformHttpClientProvider
    {
        IPlatformHttpClient GetClient(IServiceDefinition serviceDefinition);
    }

    public class PlatformHttpClientProvider : IPlatformHttpClientProvider
    {
        private readonly ISerializerFactorySelector _serializerFactorySelector;
        private readonly IServiceHttpConfigurator _serviceHttpConfigurator;
        private readonly Dictionary<string, IPlatformHttpClient> _clients = new Dictionary<string, IPlatformHttpClient>();

        public PlatformHttpClientProvider(
            ISerializerFactorySelector serializerFactorySelector,
            IEnumerable<IServiceHttpConfigurator> serviceHttpConfigurators,
            DefaultServiceHttpConfigurator defaultServiceHttpConfigurator)
        {
            _serializerFactorySelector = serializerFactorySelector;
            _serviceHttpConfigurator = serviceHttpConfigurators.FirstOrDefault() ?? defaultServiceHttpConfigurator;
        }

        public IPlatformHttpClient GetClient(IServiceDefinition serviceDefinition)
        {
            lock (_clients)
            {
                if (_clients.TryGetValue(serviceDefinition.Name, out var connector))
                    return connector;
            }

            lock (_clients)
            {
                var connector = new PlatformHttpClient(
                    serviceDefinition,
                    _serializerFactorySelector,
                    _serviceHttpConfigurator);

                _clients.Add(serviceDefinition.Name, connector);
                return connector;
            }
        }
    }
}
