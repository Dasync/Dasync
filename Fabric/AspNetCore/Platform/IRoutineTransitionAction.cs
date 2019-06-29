using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.Modeling;

namespace Dasync.AspNetCore.Platform
{
    public interface IRoutineTransitionAction
    {
        ValueTask OnRoutineStartAsync(IServiceDefinition serviceDefinition, ServiceId serviceId, RoutineMethodId methodId, string routineId);
        ValueTask OnRoutineCompleteAsync(IServiceDefinition serviceDefinition, ServiceId serviceId, RoutineMethodId methodId, string routineId, TaskResult result);
    }
}
