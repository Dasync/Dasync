using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Persistence;

namespace Dasync.EETypes.Engine
{
    public interface ILocalMethodRunner
    {
        Task<InvokeRoutineResult> RunAsync(
            IMethodInvocationData data,
            ICommunicatorMessage message,
            ISerializedMethodContinuationState continuationState);

        Task<ContinueRoutineResult> ContinueAsync(
            IMethodContinuationData data,
            ICommunicatorMessage message,
            ISerializedMethodContinuationState continuationState);
    }
}
