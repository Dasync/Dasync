using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Dasync.Communication.InMemory
{
    public class InMemoryHostedService : IHostedService
    {
        private readonly IMessageHandler _messageHandler;
        private CancellationTokenSource _stopSource = new CancellationTokenSource();
        private Task _runTask;

        public InMemoryHostedService(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runTask = _messageHandler.Run(_stopSource.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stopSource.Cancel();
            return _runTask;
        }
    }
}
