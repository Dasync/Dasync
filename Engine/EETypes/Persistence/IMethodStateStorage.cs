using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Persistence
{
    public interface IMethodStateStorage
    {
        Task WriteStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            MethodExecutionState state);

        Task<MethodExecutionState> ReadStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            CancellationToken ct);

        Task WriteResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            TaskResult result);

        Task<TaskResult> TryReadResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            Type resultValueType,
            CancellationToken ct);
    }
}
