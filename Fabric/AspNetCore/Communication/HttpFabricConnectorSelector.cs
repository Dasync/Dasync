using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.Fabric.Sample.Base;
using Dasync.Modeling;
using Dasync.Serialization;

namespace Dasync.AspNetCore.Communication
{
    public class HttpFabricConnectorSelector : IFabricConnectorSelector
    {
        private readonly ICommunicationModelProvider _communicationModelProvider;
        private readonly ISerializerFactorySelector _serializerFactorySelector;
        private readonly IServiceHttpConfigurator _serviceHttpConfigurator;
        private readonly Dictionary<string, IFabricConnector> _connectors = new Dictionary<string, IFabricConnector>();

        public HttpFabricConnectorSelector(
            ICommunicationModelProvider communicationModelProvider,
            ISerializerFactorySelector serializerFactorySelector,
            IEnumerable<IServiceHttpConfigurator> serviceHttpConfigurators)
        {
            _communicationModelProvider = communicationModelProvider;
            _serializerFactorySelector = serializerFactorySelector;
            _serviceHttpConfigurator = serviceHttpConfigurators.FirstOrDefault() ?? new DefaultServiceHttpConfigurator();
        }

        public IFabricConnector Select(ServiceId serviceId)
        {
            var serviceName = serviceId.ProxyName ?? serviceId.ServiceName;

            lock (_connectors)
            {
                if (_connectors.TryGetValue(serviceName, out var connector))
                    return connector;
            }

            var serviceDefinition = _communicationModelProvider.Model.Services.FirstOrDefault(d => d.Name == serviceName);
            if (serviceDefinition == null)
                throw new ArgumentException($"Service '{serviceName}' is not registered.");

            lock (_connectors)
            {
                var connector = new HttpFabricConnector(
                    serviceDefinition,
                    _serializerFactorySelector,
                    _serviceHttpConfigurator);

                _connectors.Add(serviceName, connector);
                return connector;
            }
        }
    }
}
