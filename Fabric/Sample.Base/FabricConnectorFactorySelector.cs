using System;
using System.Collections.Generic;

namespace Dasync.Fabric.Sample.Base
{
    public class FabricConnectorFactorySelector : IFabricConnectorFactorySelector
    {
        private readonly Dictionary<string, IFabricConnectorFactory> _factoryMap;

        public FabricConnectorFactorySelector(IFabricConnectorFactory[] factories)
        {
            _factoryMap = new Dictionary<string, IFabricConnectorFactory>(StringComparer.OrdinalIgnoreCase);
            foreach (var factory in factories)
                _factoryMap.Add(factory.ConnectorType, factory);
        }

        public IFabricConnectorFactory Select(string connectorType)
        {
            if (string.IsNullOrEmpty(connectorType))
                throw new ArgumentNullException(nameof(connectorType));

            if (_factoryMap.TryGetValue(connectorType, out var factory))
                return factory;

            throw new NotSupportedException($"No factory for a connector of type '{connectorType}'.");
        }
    }
}
