using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.ValueContainer;
using Dasync.Serialization;
using System;

namespace Dasync.ExecutionEngine.Transitions
{
    public partial class TransitionRunner
    {
        public async Task CommitAsync(
            ScheduledActions actions,
            ITransitionCarrier transitionCarrier,
            TransitionCommitOptions options,
            CancellationToken ct)
        {
            ITransitionContext context = _transitionScope.CurrentMonitor.Context;
            IMethodContinuationState continuationState = null;

            if (actions.SaveStateIntent != null)
            {
                var serviceRef = _serviceResolver.Resolve(context.Service);
                var methodRef = _methodResolver.Resolve(serviceRef.Definition, context.Method);
                var behaviorSettings = _communicationSettingsProvider.GetMethodSettings(methodRef.Definition);

                var intent = actions.SaveStateIntent;
                if (intent.RoutineState != null && intent.RoutineResult == null)
                {
                    var preferRoamingState = behaviorSettings.RoamingState;
                    var canRoamState = true; // TODO: whenall
                    var hasPersistence = false;

                    if ((preferRoamingState || !hasPersistence) && canRoamState)
                    {
                        continuationState = EncodeContinuationState(intent, transitionCarrier, context);
                    }
                    else if (!hasPersistence && !canRoamState)
                    {
                        throw new Exception("Invoking this method requires persistence for aggregation/concurrency.");
                    }
                }
            }

            if (actions.ExecuteRoutineIntents?.Count > 0)
            {
                foreach (var intent in actions.ExecuteRoutineIntents)
                {
                    var serviceRef = _serviceResolver.Resolve(intent.Service);
                    var methodRef = _methodResolver.Resolve(serviceRef.Definition, intent.Method);

                    var preferences = new InvocationPreferences();
                    var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Method);

                    IMethodContinuationState thisIntentContinuationState = null;
                    if (continuationState != null && ReferenceEquals(actions.SaveStateIntent.AwaitedRoutine, intent))
                        thisIntentContinuationState = continuationState;

                    var result = await communicator.InvokeAsync(intent, context, thisIntentContinuationState, preferences);

                    if (result.Outcome == InvocationOutcome.Complete)
                        _routineCompletionSink.OnRoutineCompleted(intent.Service, intent.Method, intent.Id, result.Result);
                }
            }

            if (actions.ResumeRoutineIntent != null)
            {
                // TODO: option to update the incoming message
                // TODO: option to lock the message and keep executing in-place
                var intent = actions.ResumeRoutineIntent;
                var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Method);
                await communicator.ContinueAsync(intent, context, continuationState, preferences: default);
            }

            if (actions.ContinuationIntents?.Count > 0)
            {
                foreach (var intent in actions.ContinuationIntents)
                {
                    var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Method);
                    await communicator.ContinueAsync(intent, context, transitionCarrier as IMethodContinuationState, preferences: default);
                }
            }

            if (actions.RaiseEventIntents?.Count > 0)
            {
                foreach (var intent in actions.RaiseEventIntents)
                {
                    var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Event);
                }
            }
        }

        private IMethodContinuationState EncodeContinuationState(
            SaveStateIntent saveStateIntent,
            ITransitionCarrier transitionCarrier,
            ITransitionContext context)
        {
            var data = new RoutineContinuationStateEnvelope
            {
                CallerDescriptor = transitionCarrier.GetContinuationsAsync(default).Result?.FirstOrDefault(),
                RoutineState = _defaultSerializer.SerializeToBytes(saveStateIntent.RoutineState),
                FlowContext = context.FlowContext
            };

            if (transitionCarrier is IMethodContinuationState continuationState)
            {
                data.CallerContentType = continuationState.ContentType;
                data.CallerState = continuationState.State;
            }

            // TODO: compress
            // TODO: encrypt
            return new MethodContinuationState
            {
                ContentType = _defaultSerializer.ContentType,
                State = _defaultSerializer.SerializeToBytes(data)
            };
        }

        private RoutineContinuationData DecodeContinuationData(IMethodContinuationState state)
        {
            if (state?.State == null || state.State.Length == 0)
                return null;

            var serializer = _serializeProvder.GetSerializer(state.ContentType);
            var routineContinuationData = serializer.Deserialize<RoutineContinuationData>(state.State);
            routineContinuationData.SetSerializer(serializer);
            return routineContinuationData;
        }
    }

    internal class MethodContinuationState : IMethodContinuationState
    {
        public string ContentType { get; set; }

        public byte[] State { get; set; }
    }

    [Serializable]
    public class RoutineContinuationStateEnvelope
    {
        // TODO: add headers for reply routing

        public ContinuationDescriptor CallerDescriptor { get; set; }

        public string CallerContentType { get; set; }

        public byte[] CallerState { get; set; }

        public byte[] RoutineState { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }
    }

    internal class RoutineContinuationData : RoutineContinuationStateEnvelope
    {
        private ISerializer _serializer;

        public void SetSerializer(ISerializer serializer) => _serializer = serializer;

        public IMethodContinuationState GetCallerContinuationState()
        {
            if (CallerState?.Length > 0)
                return new MethodContinuationState
                {
                    ContentType = CallerContentType,
                    State = CallerState
                };
            return null;
        }

        public void ReadRoutineState(IValueContainer container) => _serializer.Populate(RoutineState, container);
    }
}
