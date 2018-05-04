using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Fabric
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
    }

    public interface IFabricConnectorWithConfiguration
    {
        string ConnectorType { get; }

        object Configuration { get; }
    }
}
