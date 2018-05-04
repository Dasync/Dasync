using System;
using System.Linq;
using System.Threading;
using Dasync.EETypes;
using Dasync.EETypes.Fabric;
using Dasync.ServiceRegistry;

namespace Dasync.ExecutionEngine.Fabric
{
    public class FabricConnectorSelector : IFabricConnectorSelector
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly IFabricConnectorFactorySelector _fabricConnectorFactorySelector;
        private readonly ICurrentFabric _currentFabric;
        private readonly IServiceRegistryUpdaterViaDiscovery _serviceRegistryUpdaterViaDiscovery;

        public FabricConnectorSelector(
            IServiceRegistry serviceRegistry,
            IFabricConnectorFactorySelector fabricConnectorFactorySelector,
            ICurrentFabric currentFabric,
            IServiceRegistryUpdaterViaDiscovery serviceRegistryUpdaterViaDiscovery)
        {
            _serviceRegistry = serviceRegistry;
            _fabricConnectorFactorySelector = fabricConnectorFactorySelector;
            _currentFabric = currentFabric;
            _serviceRegistryUpdaterViaDiscovery = serviceRegistryUpdaterViaDiscovery;
        }

        public IFabricConnector Select(ServiceId serviceId)
        {
            var serviceRegistration = _serviceRegistry.AllRegistrations
                .SingleOrDefault(r => r.ServiceName == serviceId.ServiceName);

            if (serviceRegistration == null)
            {
                _serviceRegistryUpdaterViaDiscovery.UpdateAsync(CancellationToken.None).Wait();
                serviceRegistration = _serviceRegistry.AllRegistrations
                    .SingleOrDefault(r => r.ServiceName == serviceId.ServiceName);

                if (serviceRegistration == null)
                    throw new InvalidOperationException($"Service '{serviceId.ServiceName}' is not registered.");
            }

            if (!serviceRegistration.IsExternal)
            {
                var connector = _currentFabric.Instance.GetConnector(serviceId);
                if (connector == null)
                    throw new InvalidOperationException(
                        $"The fabric does not have a self-connector for service '{serviceId.ServiceName}'.");
                return connector;
            }

            var connectorType = serviceRegistration.ConnectorType;
            var connectorConfiguration = serviceRegistration.ConnectorConfiguration;

            var fabricConnectorFactory = _fabricConnectorFactorySelector.Select(connectorType);
            var fabricConnector = fabricConnectorFactory.Create(serviceId, connectorConfiguration);

            return fabricConnector;
        }
    }
}
