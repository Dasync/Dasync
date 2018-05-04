using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Transitions
{
    public interface ITransitionData
    {
        Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct);

        Task<ServiceId> GetServiceIdAsync(CancellationToken ct);

        Task<RoutineDescriptor> GetRoutineDescriptorAsync(CancellationToken ct);

        Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct);

        Task ReadServiceStateAsync(IValueContainer target, CancellationToken ct);

        Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct);

        Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct);

        Task<RoutineResultDescriptor> GetAwaitedResultAsync(CancellationToken ct);
    }
}
