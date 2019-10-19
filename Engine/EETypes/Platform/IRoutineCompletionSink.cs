using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Platform
{
    public interface IRoutineCompletionSink
    {
        void OnRoutineCompleted(ServiceId serviceId, MethodId methodId, string intentId, TaskResult taskResult);
    }
}
