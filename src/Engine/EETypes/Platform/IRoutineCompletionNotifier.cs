using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Platform
{
    public interface IRoutineCompletionNotifier
    {
        Task<ITaskResult> TryPollCompletionAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            CancellationToken ct);

        long NotifyOnCompletion(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            TaskCompletionSource<ITaskResult> completionSink,
            CancellationToken ct);

        bool StopTracking(long token);
    }
}
