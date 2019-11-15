using Dasync.EETypes.Communication;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Communication.RabbitMQ
{
    public class RabbitMQCommunicationMethod : ICommunicationMethod, IEventingMethod
    {
        public const string MethodType = "RabbitMQ";

        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IConnectionManager _connectionManager;

        public RabbitMQCommunicationMethod(
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider,
            IConnectionManager connectionManager)
        {
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializerProvider = serializerProvider;
            _connectionManager = connectionManager;
        }

        public string Type => MethodType;

        public ICommunicator CreateCommunicator(IConfiguration configuration)
        {
            var connectionSettings = new ConnectionSettings();
            configuration.Bind(connectionSettings);

            var communicatorSettings = CreateMethodsDefaultSettings();
            configuration.Bind(communicatorSettings);

            var serializer = SelectSerializer(communicatorSettings.Serializer);
            var connection = _connectionManager.GetConnection(connectionSettings);

            return new RabbitMQCommunicator(connection, serializer, communicatorSettings);
        }

        public IEventPublisher CreateEventPublisher(IConfiguration configuration)
        {
            var connectionSettings = new ConnectionSettings();
            configuration.Bind(connectionSettings);

            var publisherSettings = CreateEventsDefaultSettings();
            configuration.Bind(publisherSettings);

            var serializer = SelectSerializer(publisherSettings.Serializer);
            var connection = _connectionManager.GetConnection(connectionSettings);

            return new RabbitMQCommunicator(connection, serializer, publisherSettings);
        }

        private ISerializer SelectSerializer(string settingValue)
        {
            return string.IsNullOrWhiteSpace(settingValue)
                ? _defaultSerializer
                : _serializerProvider.GetSerializer(settingValue);
        }

        public static CommunicationSettings CreateMethodsDefaultSettings()
        {
            return new CommunicationSettings
            {
                QueueName = "{serviceName}"
            };
        }

        public static CommunicationSettings CreateEventsDefaultSettings()
        {
            return new CommunicationSettings
            {
                ExchangeName = "{serviceName}"
            };
        }
    }
}
