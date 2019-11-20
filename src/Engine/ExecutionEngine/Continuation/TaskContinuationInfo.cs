using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.ExecutionEngine.Continuation
{
    public struct TaskContinuationInfo
    {
        public TaskContinuationType Type;
        public object Target;
        public ICollection Items;
        public ExecutionContext CapturedContext;
        public TaskContinuationOptions Options;
    }
}
