using System;
using Dasync.AzureStorage;
using Dasync.EETypes;
using Dasync.EETypes.Fabric;
using Dasync.Serialization;

namespace Dasync.FabricConnector.AzureStorage
{
    public class AzureStorageFabricConnectorFactory : IFabricConnectorFactory
    {
        private readonly ISerializerFactorySelector _serializerFactorySelector;
        private readonly INumericIdGenerator _idGenerator;
        private readonly ICloudStorageAccountFactory _cloudStorageAccountFactory;
        private readonly IStorageAccontConnectionStringResolver _storageAccontConnectionStringResolver;

        public AzureStorageFabricConnectorFactory(
            ISerializerFactorySelector serializerFactorySelector,
            INumericIdGenerator idGenerator,
            ICloudStorageAccountFactory cloudStorageAccountFactory,
            IStorageAccontConnectionStringResolver storageAccontConnectionStringResolver)
        {
            _serializerFactorySelector = serializerFactorySelector;
            _idGenerator = idGenerator;
            _cloudStorageAccountFactory = cloudStorageAccountFactory;
            _storageAccontConnectionStringResolver = storageAccontConnectionStringResolver;
        }

        public string ConnectorType => "AzureStorage";

        public IFabricConnector Create(ServiceId serviceId, object configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var config = (AzureStorageFabricConnectorConfiguration)configuration;

            var serializerFactory = _serializerFactorySelector.Select(config.SerializerFormat);
            var serializer = serializerFactory.Create();

            var connectionString = _storageAccontConnectionStringResolver.Resolve(config.StorageAccountName);
            var storageAccount = _cloudStorageAccountFactory.Create(connectionString);
            var transitionsQueue = storageAccount.QueueClient.GetQueueReference(config.TransitionsQueueName);
            var routinesTable = storageAccount.TableClient.GetTableReference(config.RoutinesTableName);

            return new AzureStorageFabricConnectorWithConfiguration(
                serviceId, _idGenerator, transitionsQueue, routinesTable, serializer, config);
        }
    }
}
