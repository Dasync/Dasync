using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;

namespace Dasync.Communication.InMemory
{
    public class InMemoryMessageListener : IMessageListener
    {
        private readonly IMessageHandler _messageHandler;
        private CancellationTokenSource _stopSource = new CancellationTokenSource();
        private Task _runTask;

        public InMemoryMessageListener(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }

        public Task StartAsync(CancellationToken ct)
        {
            if (_runTask == null)
                _runTask = _messageHandler.Run(_stopSource.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            var result = _runTask;

            if (result == null)
                return Task.CompletedTask;

            _runTask = null;
            _stopSource.Cancel();
            return result;
        }
    }
}
