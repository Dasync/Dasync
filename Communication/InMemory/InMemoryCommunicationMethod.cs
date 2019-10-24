using Dasync.EETypes.Communication;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Communication.InMemory
{
    public class InMemoryCommunicationMethod : ICommunicationMethod
    {
        public const string MethodType = "InMemory";

        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IMessageHub _messageHub;
        private readonly IMethodStateStorageProvider _methodStateStorageProvider;

        public InMemoryCommunicationMethod(
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider,
            IMessageHub messageHub,
            IMethodStateStorageProvider methodStateStorageProvider)
        {
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializerProvider = serializerProvider;
            _messageHub = messageHub;
            _methodStateStorageProvider = methodStateStorageProvider;
        }

        public string Type => MethodType;

        public ICommunicator CreateCommunicator(IConfiguration configuration)
        {
            var serializationFormat = configuration.GetSection("serializer").Value;
            var serializer = string.IsNullOrWhiteSpace(serializationFormat)
                ? _defaultSerializer
                : _serializerProvider.GetSerializer(serializationFormat);

            return new InMemoryCommunicator(serializer, _messageHub, _methodStateStorageProvider);
        }
    }
}
