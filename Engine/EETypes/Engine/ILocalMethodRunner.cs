using System.Threading.Tasks;
using Dasync.EETypes.Communication;

namespace Dasync.EETypes.Engine
{
    public interface ILocalMethodRunner
    {
        Task<InvokeRoutineResult> RunAsync(
            MethodInvocationData data,
            ICommunicatorMessage message);

        Task<ContinueRoutineResult> ContinueAsync(
            MethodContinuationData data,
            ICommunicatorMessage message);

        Task ReactAsync(
            EventPublishData data,
            ICommunicatorMessage message);
    }
}
