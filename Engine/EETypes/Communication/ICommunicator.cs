using System.Threading.Tasks;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Communication
{
    public interface ICommunicator
    {
        /// <summary>
        /// Unique name of the method, e.g. 'HTTP', 'AzureEventHub', 'AzureServiceBus', 'AzureStorageQueue'
        /// </summary>
        string Type { get; }

        CommunicationTraits Traits { get; }


        // send execute routine request
        // - sync (result is available right away)
        // - async (no ability to send reply into the same process except for polling w/ a sync method)
        // - optionally include continuation data

        Task<InvokeRoutineResult> InvokeAsync(
            ExecuteRoutineIntent intent,
            ITransitionContext context,
            IMethodContinuationState continuationState,
            InvocationPreferences preferences);

        // reply w/ result (invoke continuation)
        // - must be able to access the requestor's communication
        // - must share configuration / conventions
        // - otherwise sync response or future web-hook thing

        Task<ContinueRoutineResult> ContinueAsync(
            ContinueRoutineIntent intent,
            ITransitionContext context,
            IMethodContinuationState continuationState,
            InvocationPreferences preferences);

        // publish event
        // - fire and forget
    }
}
