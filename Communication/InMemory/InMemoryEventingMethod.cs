using Dasync.EETypes.Communication;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Communication.InMemory
{
    public class InMemoryEventingMethod : IEventingMethod
    {
        public const string MethodType = "InMemory";

        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IMessageHub _messageHub;

        public InMemoryEventingMethod(
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider,
            IMessageHub messageHub)
        {
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializerProvider = serializerProvider;
            _messageHub = messageHub;
        }

        public string Type => MethodType;

        public IEventPublisher CreateEventPublisher(IConfiguration configuration)
        {
            var serializationFormat = configuration.GetSection("serializer").Value;
            var serializer = string.IsNullOrWhiteSpace(serializationFormat)
                ? _defaultSerializer
                : _serializerProvider.GetSerializer(serializationFormat);

            return new InMemoryEventPublisher(serializer, _messageHub);
        }
    }
}
