using System;
using Dasync.EETypes;
using Dasync.EETypes.Fabric;
using Dasync.Serialization;

namespace Dasync.Fabric.InMemory
{
    public class InMemoryFabricConnectorFactory : IFabricConnectorFactory
    {
        private readonly ISerializerFactorySelector _serializerFactorySelector;

        public InMemoryFabricConnectorFactory(ISerializerFactorySelector serializerFactorySelector)
        {
            _serializerFactorySelector = serializerFactorySelector;
        }

        public string ConnectorType => "InMemory";

        public IFabricConnector Create(ServiceId serviceId, object configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var config = (InMemoryFabricConnectorConfiguration)configuration;

            if (!InMemoryDataStore.TryGet(config.DataStoreId, out var dataStore))
                throw new InvalidOperationException($"In-memory data store with ID '{config.DataStoreId}' does not exist.");

            var serializerFactory = _serializerFactorySelector.Select(config.SerializerFormat);
            var serializer = serializerFactory.Create();

            return new InMemoryFabricConnector(dataStore, serializer, config.SerializerFormat);
        }
    }

    public class InMemoryFabricConnectorConfiguration
    {
        public int DataStoreId { get; set; }

        public string SerializerFormat { get; set; }
    }
}
