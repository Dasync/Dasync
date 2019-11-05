using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.Serialization;

namespace Dasync.Communication.InMemory
{
    public class InMemoryEventPublisher : IEventPublisher
    {
        private readonly ISerializer _serializer;
        private readonly IMessageHub _messageHub;

        public InMemoryEventPublisher(
            ISerializer serializer,
            IMessageHub messageHub)
        {
            _serializer = serializer;
            _messageHub = messageHub;
        }

        public string Type => InMemoryEventingMethod.MethodType;

        public CommunicationTraits Traits =>
            CommunicationTraits.Volatile |
            CommunicationTraits.MessageLockOnPublish;

        public Task PublishAsync(EventPublishData data)
        {
            var message = new Message
            {
                Type = MessageType.Event,
            };

            EventPublishDataTransformer.Write(message, data, _serializer);

            _messageHub.Schedule(message);

            return Task.CompletedTask;
        }
    }
}
