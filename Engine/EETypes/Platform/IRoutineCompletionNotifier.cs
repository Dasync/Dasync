using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Platform
{
    public interface IRoutineCompletionNotifier
    {
        void NotifyCompletion(long intentId, TaskCompletionSource<TaskResult> completionSink);
    }
}
