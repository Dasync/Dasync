using System;
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
            // Carrier is NULL when call is made outside of a transition scope, e.g. from entry point of a console app.
            ITransitionCarrier transitionCarrier,
            TransitionCommitOptions options,
            CancellationToken ct)
        {
#warning This need deep thinking on how to achieve consistency

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
                    if (options.NotifyOnRoutineCompletion)
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

            if (actions.RaiseEventIntents?.Count > 0)
            {
                foreach (var intent in actions.RaiseEventIntents)
                {
                    if (intent.ServiceId == null)
                        throw new NotSupportedException();

                    var connector = _fabricConnectorSelector.Select(intent.ServiceId);
                    await connector.PublishEventAsync(intent, ct);
                }
            }

            if (actions.RegisterTriggerIntents?.Count > 0)
            {
                var connector = ((ICurrentConnectorProvider)transitionCarrier).Connector;
                foreach (var intent in actions.RegisterTriggerIntents)
                    await connector.RegisterTriggerAsync(intent, ct);
            }

            if (actions.ActivateTriggerIntents?.Count > 0)
            {
                var connector = ((ICurrentConnectorProvider)transitionCarrier).Connector;
                foreach (var intent in actions.ActivateTriggerIntents)
                    await connector.ActivateTriggerAsync(intent, ct);
            }

            if (actions.SubscribeToTriggerIntents?.Count > 0)
            {
                var connector = ((ICurrentConnectorProvider)transitionCarrier).Connector;
                foreach (var intent in actions.SubscribeToTriggerIntents)
                    await connector.SubscribeToTriggerAsync(intent, ct);
            }
        }
    }
}
