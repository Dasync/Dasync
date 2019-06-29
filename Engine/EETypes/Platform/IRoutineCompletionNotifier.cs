using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Platform
{
    public interface IRoutineCompletionNotifier
    {
        Task<TaskResult> TryPollCompletionAsync(
            ServiceId serviceId,
            RoutineMethodId methodId,
            string intentId,
            CancellationToken ct);

        void NotifyCompletion(
            ServiceId serviceId,
            RoutineMethodId methodId,
            string intentId,
            TaskCompletionSource<TaskResult> completionSink,
            CancellationToken ct);
    }
}
