using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Platform
{
    /// <summary>
    /// Must be implemented by concrete platform.
    /// </summary>
    public interface ITransitionCarrier
    {
        Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct);

        Task<ServiceId> GetServiceIdAsync(CancellationToken ct);

        Task<RoutineDescriptor> GetRoutineDescriptorAsync(CancellationToken ct);

        Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct);

        Task ReadServiceStateAsync(IValueContainer target, CancellationToken ct);

        Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct);

        Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct);

        Task<ResultDescriptor> GetAwaitedResultAsync(CancellationToken ct);
    }
}
