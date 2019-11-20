using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class MessageListeningMethod : IMessageListeningMethod
    {
        public string Type => "http";

        public Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IMethodDefinition, IConfiguration> methodConfigMap,
            CancellationToken ct)
        {
            return Task.FromResult<IEnumerable<IMessageListener>>(Array.Empty<IMessageListener>());
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
