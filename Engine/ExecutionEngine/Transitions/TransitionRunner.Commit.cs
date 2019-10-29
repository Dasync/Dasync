using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.Serialization;
using Dasync.ValueContainer;

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
            ISerializedMethodContinuationState continuationState = null;

            if (actions.SaveStateIntent != null)
            {
                var intent = actions.SaveStateIntent;
                var serviceRef = _serviceResolver.Resolve(context.Service);
                var methodRef = _methodResolver.Resolve(serviceRef.Definition, context.Method);
                var behaviorSettings = _communicationSettingsProvider.GetMethodSettings(methodRef.Definition);

                IMethodStateStorage stateStorage = null;

                if (intent.RoutineState != null && intent.RoutineResult == null)
                {
                    var roamState = behaviorSettings.RoamingState;

                    if (!roamState)
                    {
                        stateStorage = _methodStateStorageProvider.GetStorage(context.Service, context.Method, returnNullIfNotFound: true);
                        if (stateStorage == null)
                        {
                            // Fallback: try to roam the state if possible
                            roamState = true;
                        }
                    }

                    if (roamState)
                    {
                        // Aggregate methods (WhenAll, WhenAny) must have a concurrency control
                        // using a persistence mechanism to aggregate multiple results.
                        roamState = methodRef.Definition.FindProperty("aggregate")?.Value != (object)true;
                    }

                    if (!roamState && stateStorage == null)
                    {
                        throw new InvalidOperationException($"Invoking method '{methodRef.Id}' on '{serviceRef.Id}' requires persistence for aggregating result.");
                    }

                    if (roamState)
                    {
                        continuationState = EncodeContinuationState(intent, transitionCarrier, context);
                    }
                    else
                    {
                        try
                        {
                            await stateStorage.WriteStateAsync(
                                actions.SaveStateIntent.Service,
                                actions.SaveStateIntent.Method,
                                context,
                                actions.SaveStateIntent.RoutineState,
                                transitionCarrier.GetContinuationsAsync(default).Result?.FirstOrDefault(),
                                transitionCarrier as ISerializedMethodContinuationState);
                        }
                        catch (ETagMismatchException ex)
                        {
                            // The record in the storage has changed since the beginning of the transition.
                            // There must have been a concurrent transition.
                            // Discard current results and try again.
                            throw new ConcurrentTransitionException(ex);
                        }
                    }
                }
                else if (intent.RoutineResult != null)
                {
                    // TODO: make this behavior optional if a continuation is present.
                    // TODO: for the event sourcing style, the result must be written by the receiver of the response.
                    var writeResult = true;

                    if (stateStorage == null)
                        stateStorage = _methodStateStorageProvider.GetStorage(context.Service, context.Method, returnNullIfNotFound: true);

                    if (stateStorage == null)
                    {
                        // Fallback: if the method has a continuation, assume that no polling is expected,
                        // so there is no need to write the result into a persisted storage.
                        // This does not cover 'fire and forget' scenarios.
                        if (transitionCarrier.GetContinuationsAsync(default).Result?.Count > 0)
                        {
                            writeResult = false;
                        }
                        else
                        {
                            throw new InvalidOperationException($"The method '{methodRef.Id}' of '{serviceRef.Id}' must have persistence for storing its result.");
                        }
                    }

                    if (writeResult)
                    {
                        await stateStorage.WriteResultAsync(
                            intent.Service,
                            intent.Method,
                            context.IntentId,
                            intent.RoutineResult);
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

                    ISerializedMethodContinuationState thisIntentContinuationState = null;
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
                    await communicator.ContinueAsync(intent, context, transitionCarrier as ISerializedMethodContinuationState, preferences: default);
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

        private ISerializedMethodContinuationState EncodeContinuationState(
            SaveStateIntent saveStateIntent,
            ITransitionCarrier transitionCarrier,
            ITransitionContext context)
        {
            var data = new MethodExecutionStateDto
            {
                Service = saveStateIntent.Service,
                Method = saveStateIntent.Method,
                Caller = context.Caller,
                Continuation = transitionCarrier.GetContinuationsAsync(default).Result?.FirstOrDefault(),
                Format = _defaultSerializer.Format,
                MethodStateData = _defaultSerializer.SerializeToBytes(saveStateIntent.RoutineState),
                FlowContext = context.FlowContext
            };

            if (transitionCarrier is ISerializedMethodContinuationState continuationState)
            {
                data.ContinuationStateFormat = continuationState.Format;
                data.ContinuationStateData = continuationState.State;
            }

            // TODO: compress
            // TODO: encrypt
            return new MethodContinuationState
            {
                Format = _defaultSerializer.Format,
                State = _defaultSerializer.SerializeToBytes(data)
            };
        }

        private IMethodExecutionState DecodeContinuationData(ISerializedMethodContinuationState state)
        {
            if (state?.State == null || state.State.Length == 0)
                return null;

            var serializer = _serializeProvder.GetSerializer(state.Format);
            var dto = serializer.Deserialize<MethodExecutionStateDto>(state.State);
            return new MethodExecutionState(dto, _serializeProvder);
        }
    }

    internal class MethodContinuationState : ISerializedMethodContinuationState
    {
        public string Format { get; set; }

        public byte[] State { get; set; }
    }

    [Serializable]
    public class MethodExecutionStateDto
    {
        // TODO: add headers for reply routing

        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public string Format { get; set; }

        public byte[] MethodStateData { get; set; }

        public string ContinuationStateFormat { get; set; }

        public byte[] ContinuationStateData { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }
    }

    internal class MethodExecutionState : IMethodExecutionState
    {
        public MethodExecutionState(
            MethodExecutionStateDto dto,
            ISerializerProvider serializerProvider)
        {
            Service = dto.Service;
            Method = dto.Method;
            Continuation = dto.Continuation;
            FlowContext = dto.FlowContext;
            Caller = dto.Caller;
            MethodStateData = dto.MethodStateData;
            Serializer = serializerProvider.GetSerializer(dto.Format);

            CallerState = (dto.ContinuationStateData?.Length > 0)
                ? new MethodContinuationState
                {
                    Format = dto.ContinuationStateFormat,
                    State = dto.ContinuationStateData
                }
                : null;
        }

        private ISerializer Serializer { get; }

        public ServiceId Service { get; }

        public PersistedMethodId Method { get; }

        public ContinuationDescriptor Continuation { get; }

        public byte[] MethodStateData { get; }

        public CallerDescriptor Caller { get; }

        public Dictionary<string, string> FlowContext { get; }

        public ISerializedMethodContinuationState CallerState { get; }

        public void ReadMethodState(IValueContainer container) =>
            Serializer.Populate(MethodStateData, container);
    }
}
