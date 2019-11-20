using Dasync.EETypes.Communication;

namespace Dasync.Communication.InMemory
{
    public class CommunicationMessage : ICommunicatorMessage
    {
        public CommunicationMessage(Message message)
        {
            Message = message;
        }
        public Message Message { get; }

        public string CommunicatorType => InMemoryCommunicationMethod.MethodType;

        public CommunicationTraits CommunicatorTraits =>
            CommunicationTraits.Volatile |
            CommunicationTraits.MessageLockOnPublish |
            (WaitForResult ? CommunicationTraits.SyncReplies : default);

        public bool? IsRetry => Message.DeliveryCount > 1;

        public string RequestId => Message.Id;

        public bool WaitForResult { get; set; }
    }
}
