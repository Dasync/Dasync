using System.Threading.Tasks;
using Dasync.EETypes.Persistence;

namespace Dasync.EETypes.Communication
{
    public interface ICommunicator
    {
        /// <summary>
        /// Unique name of the method, e.g. 'HTTP', 'AzureEventHub', 'AzureServiceBus', 'AzureStorageQueue'
        /// </summary>
        string Type { get; }

        CommunicationTraits Traits { get; }

        Task<InvokeRoutineResult> InvokeAsync(
            MethodInvocationData data,
            SerializedMethodContinuationState continuationState,
            InvocationPreferences preferences);

        Task<ContinueRoutineResult> ContinueAsync(
            MethodContinuationData data,
            SerializedMethodContinuationState continuationState,
            InvocationPreferences preferences);
    }
}
