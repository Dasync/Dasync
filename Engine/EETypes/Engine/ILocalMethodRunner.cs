using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Persistence;

namespace Dasync.EETypes.Engine
{
    public interface ILocalMethodRunner
    {
        Task<InvokeRoutineResult> RunAsync(
            MethodInvocationData data,
            ICommunicatorMessage message,
            SerializedMethodContinuationState continuationState);

        Task<ContinueRoutineResult> ContinueAsync(
            MethodContinuationData data,
            ICommunicatorMessage message,
            SerializedMethodContinuationState continuationState);
    }
}
