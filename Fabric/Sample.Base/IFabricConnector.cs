using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;

namespace Dasync.Fabric.Sample.Base
{
    public interface IFabricConnector
    {
        // TODO: transactionality
        //bool TryPreGenerateRoutineId(ExecuteRoutineIntent intent, out string routineId);

        Task<ActiveRoutineInfo> ScheduleRoutineAsync(ExecuteRoutineIntent intent, CancellationToken ct);

        Task<ActiveRoutineInfo> PollRoutineResultAsync(ActiveRoutineInfo info, CancellationToken ct);

        Task<ActiveRoutineInfo> ScheduleContinuationAsync(ContinueRoutineIntent intent, CancellationToken ct);

        // TODO: dynamic continuation
        //Task AddContinuationAsync(RoutineDescriptor routineDescriptor, CancellationToken ct);

        Task SubscribeForEventAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector publisherFabricConnector);

        Task OnEventSubscriberAddedAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector subsriberFabricConnector);

        Task PublishEventAsync(RaiseEventIntent intent, CancellationToken ct);
    }

    public interface IFabricConnectorWithConfiguration
    {
        string ConnectorType { get; }

        object Configuration { get; }
    }
}
