using Dasync.EETypes.Communication;

namespace Dasync.Communication.RabbitMQ
{
    public class CommunicationMessage : ICommunicatorMessage
    {
        public string CommunicatorType => RabbitMQCommunicationMethod.MethodType;

        public CommunicationTraits CommunicatorTraits => default;

        public bool? IsRetry => null;

        public string RequestId => null;
    }
}
