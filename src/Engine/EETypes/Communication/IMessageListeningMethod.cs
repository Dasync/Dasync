using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.EETypes.Communication
{
    public interface IMessageListeningMethod
    {
        string Type { get; }

        Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IMethodDefinition, IConfiguration> methodConfigMap,
            CancellationToken ct);

        // TODO: pass configuration of subscribers?
        Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IEventDefinition, IConfiguration> eventConfigMap,
            CancellationToken ct);
    }
}
