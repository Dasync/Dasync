using System.Threading.Tasks;
using Dasync.EETypes.Communication;

namespace Dasync.Communication.InMemory
{
    public class MessageHandle : IMessageHandle
    {
        private Message _message;
        private IMessageHub _messageHub;

        public MessageHandle(Message message, IMessageHub messageHub)
        {
            _message = message;
            _messageHub = messageHub;
        }

        public string Id => _message?.Id;

        public Task Complete()
        {
            _message = null;
            _messageHub = null;
            return Task.CompletedTask;
        }

        public void ReleaseLock()
        {
            if (_messageHub == null || _message == null)
                return;

            _messageHub.Schedule(_message);
            _message = null;
            _messageHub = null;
        }
    }
}
