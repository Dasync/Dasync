using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;

namespace Dasync.Fabric.Sample.Base
{
    public class TransitionCommitter : ITransitionCommitter
    {
        private readonly IFabricConnectorSelector _fabricConnectorSelector;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;

        public TransitionCommitter(
            IFabricConnectorSelector fabricConnectorSelector,
            IRoutineCompletionNotifier routineCompletionNotifier)
        {
            _fabricConnectorSelector = fabricConnectorSelector;
            _routineCompletionNotifier = routineCompletionNotifier;
        }

        public async Task CommitAsync(
            ScheduledActions actions,
            ITransitionCarrier transitionCarrier,
            CancellationToken ct)
        {
#warning This need deep thinking on how to achieve transictionality

            if (actions.SaveStateIntent != null)
            {
#warning Make sure that saving service and routine state is transactional - you don't want to re-run routine on failure after service state was saved only.
                var intent = actions.SaveStateIntent;
                await ((ITransitionStateSaver)transitionCarrier).SaveStateAsync(intent, ct);
            }

            if (actions.ExecuteRoutineIntents?.Count > 0)
            {
                foreach (var intent in actions.ExecuteRoutineIntents)
                {
                    var connector = _fabricConnectorSelector.Select(intent.ServiceId);
#warning TODO: try to pre-generate routine ID - needed for transactionality.
#warning TODO: check if target fabric can route back the continuation. If not, come up with another strategy, e.g. polling, or gateway?
                    var info = await connector.ScheduleRoutineAsync(intent, ct);
#warning TODO: check if routine is already done - it's possible on retry to run the transition, or under some special circumstances.
#warning TODO: save scheduled routine info into current routine's state - needed for dynamic subscription.
                    if (intent.NotifyOnCompletion)
                        ((IInternalRoutineCompletionNotifier)_routineCompletionNotifier).RegisterComittedRoutine(intent.Id, connector, info);
                }
            }

            if (actions.ResumeRoutineIntent != null)
            {
#warning need ability to overwrite existing message instead of creating a new one (if supported)
                var intent = actions.ResumeRoutineIntent;
                var connector = _fabricConnectorSelector.Select(intent.Continuation.ServiceId);
                var info = await connector.ScheduleContinuationAsync(intent, ct);
            }

            if (actions.ContinuationIntents?.Count > 0)
            {
                foreach (var intent in actions.ContinuationIntents)
                {
                    var connector = _fabricConnectorSelector.Select(intent.Continuation.ServiceId);
                    var info = await connector.ScheduleContinuationAsync(intent, ct);
                }
            }
        }
    }
}
