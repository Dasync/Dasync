using System;
using Dasync.EETypes;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;

namespace Dasync.Fabric.FileBased
{
    public class FileBasedFabricConnectorFactory : IFabricConnectorFactory
    {
        private readonly ISerializerFactorySelector _serializerFactorySelector;
        private readonly IUniqueIdGenerator _idGenerator;

        public FileBasedFabricConnectorFactory(
            ISerializerFactorySelector serializerFactorySelector,
            IUniqueIdGenerator idGenerator)
        {
            _serializerFactorySelector = serializerFactorySelector;
            _idGenerator = idGenerator;
        }

        public string ConnectorType => "FileBased";

        public IFabricConnector Create(ServiceId serviceId, object configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var config = (FileBasedFabricConnectorConfiguration)configuration;

            var serializerFactory = _serializerFactorySelector.Select(config.SerializerFormat);
            var serializer = serializerFactory.Create();

            return new FileBasedFabricConnector(
                _idGenerator,
                config.TransitionsDirectory,
                config.RoutinesDirectory,
                config.EventsDirectory,
                null,
                serializer,
                config.SerializerFormat);
        }
    }

    public class FileBasedFabricConnectorConfiguration
    {
        public string TransitionsDirectory { get; set; }

        public string RoutinesDirectory { get; set; }

        public string EventsDirectory { get; set; }

        public string SerializerFormat { get; set; }
    }
}
