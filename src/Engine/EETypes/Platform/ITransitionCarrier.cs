using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Platform
{
    [Obsolete]
    public interface ITransitionCarrier
    {
        [Obsolete]
        Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct);

        [Obsolete]
        Task<ServiceId> GetServiceIdAsync(CancellationToken ct);

        [Obsolete]
        Task<PersistedMethodId> GetRoutineDescriptorAsync(CancellationToken ct);

        [Obsolete]
        Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct);

        //Task ReadServiceStateAsync(IValueContainer target, CancellationToken ct);

        [Obsolete]
        Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct);

        [Obsolete]
        Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct);

        [Obsolete]
        string ResultTaskId { get; }

        [Obsolete]
        ITaskResult ReadResult(Type expectedResultValueType);
    }
}
