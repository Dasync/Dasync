using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.Communication.InMemory
{
    public class InMemoryMessageListeningMethod : IMessageListeningMethod
    {
        public string Type => InMemoryCommunicationMethod.MethodType;

        private readonly InMemoryMessageListener _sinleListener;

        public InMemoryMessageListeningMethod(IMessageHandler messageHandler)
        {
            _sinleListener = new InMemoryMessageListener(messageHandler);
        }

        public async Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IMethodDefinition, IConfiguration> methodConfigMap,
            CancellationToken ct)
        {
            await _sinleListener.StartAsync(ct);
            return new[] { _sinleListener };
        }

        public Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IEventDefinition, IConfiguration> eventConfigMap,
            CancellationToken ct)
        {
            return Task.FromResult<IEnumerable<IMessageListener>>(Array.Empty<IMessageListener>());
        }
    }
}
