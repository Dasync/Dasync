using System.Threading.Tasks;
using Dasync.EETypes.Communication;

namespace Dasync.EETypes.Engine
{
    public interface ILocalMethodRunner
    {
        Task<InvokeRoutineResult> RunAsync(
            IMethodInvocationData data,
            ICommunicatorMessage message,
            IMethodContinuationState continuationState);

        Task<ContinueRoutineResult> ContinueAsync(
            IMethodContinuationData data,
            ICommunicatorMessage message,
            IMethodContinuationState continuationState);
    }
}
