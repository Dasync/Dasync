using System;
using Dasync.EETypes;
using Dasync.EETypes.Fabric;
using Dasync.Serialization;

namespace Dasync.Fabric.FileBased
{
    public class FileBasedFabricConnectorFactory : IFabricConnectorFactory
    {
        private readonly ISerializerFactorySelector _serializerFactorySelector;

        public FileBasedFabricConnectorFactory(ISerializerFactorySelector serializerFactorySelector)
        {
            _serializerFactorySelector = serializerFactorySelector;
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
                config.TransitionsDirectory,
                config.RoutinesDirectory,
                serializer,
                config.SerializerFormat);
        }
    }

    public class FileBasedFabricConnectorConfiguration
    {
        public string TransitionsDirectory { get; set; }

        public string RoutinesDirectory { get; set; }

        public string SerializerFormat { get; set; }
    }
}
