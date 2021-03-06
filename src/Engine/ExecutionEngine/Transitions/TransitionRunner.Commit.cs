﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Utils;
using Dasync.Serialization;

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
            SerializedMethodContinuationState continuationState = null;

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
                            var executionState = GetMethodExecutionState(
                                actions.SaveStateIntent, transitionCarrier, context);

                            var etag = await stateStorage.WriteStateAsync(
                                actions.SaveStateIntent.Service,
                                actions.SaveStateIntent.Method,
                                executionState);

                            // NOTE: assume that ContinuationDescriptor instances refer to this instance of
                            // PersistedMethodId and the ETag gets automatically propagated to the invoke intents.
                            actions.SaveStateIntent.Method.ETag = etag;
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
                    var expectsSyncReply = (transitionCarrier as TransitionCarrier)?.Message.CommunicatorTraits.HasFlag(CommunicationTraits.SyncReplies) == true;
                    
                    // TODO: make this behavior optional if a continuation is present (no polling expected).
                    // TODO: for the event sourcing style, the result must be written by the receiver of the response.
                    var writeResult = !expectsSyncReply;

                    if (writeResult && context.Caller?.Event != null)
                        writeResult = false;

                    if (writeResult && stateStorage == null)
                        stateStorage = _methodStateStorageProvider.GetStorage(context.Service, context.Method, returnNullIfNotFound: true);

                    if (stateStorage == null)
                    {
                        // Fallback (A): if the method has a continuation, assume that no polling is expected,
                        // so there is no need to write the result into a persisted storage.
                        // This does not cover 'fire and forget' scenarios.
                        // Fallback (B): This method must be an event handler - no need
                        // to write result because nothing should poll for the result.
                        if (expectsSyncReply
                            || transitionCarrier.GetContinuationsAsync(default).Result?.Count > 0
                            || context.Caller?.Event != null)
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

                    var resultValueType = methodRef.Definition.MethodInfo.ReturnType;
                    if (resultValueType != typeof(void))
                    {
                        resultValueType = TaskAccessor.GetTaskResultType(resultValueType);
                        if (resultValueType == TaskAccessor.VoidTaskResultType)
                            resultValueType = typeof(void);
                    }

                    var preferences = new InvocationPreferences
                    {
                        ResultValueType = resultValueType
                    };

                    var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Method);

                    var invocationData = InvocationDataUtils.CreateMethodInvocationData(intent, context);

                    if (continuationState != null && ReferenceEquals(actions.SaveStateIntent.AwaitedRoutine, intent))
                        invocationData.ContinuationState = continuationState;

                    var result = await communicator.InvokeAsync(invocationData, preferences);

                    if (result.Outcome == InvocationOutcome.Complete)
                        _routineCompletionSink.OnRoutineCompleted(intent.Service, intent.Method, intent.Id, result.Result);
                }
            }

            if (actions.ResumeRoutineIntent != null)
            {
                // TODO: option to update the incoming message
                // TODO: option to lock the message and keep executing in-place
                var intent = actions.ResumeRoutineIntent;
                var continuationData = InvocationDataUtils.CreateMethodContinuationData(intent, context);
                continuationData.State = continuationState;
                var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Method);
                await communicator.ContinueAsync(continuationData, preferences: default);
            }

            if (actions.ContinuationIntents?.Count > 0)
            {
                foreach (var intent in actions.ContinuationIntents)
                {
                    var continuationData = InvocationDataUtils.CreateMethodContinuationData(intent, context);
                    continuationData.State = (transitionCarrier as TransitionCarrier)?.ContinuationState;
                    var communicator = _communicatorProvider.GetCommunicator(intent.Service, intent.Method, assumeExternal: true);
                    await communicator.ContinueAsync(continuationData, preferences: default);
                }
            }

            if (actions.RaiseEventIntents?.Count > 0)
            {
                foreach (var intent in actions.RaiseEventIntents)
                {
                    var eventData = new EventPublishData
                    {
                        IntentId = intent.Id,
                        Service = intent.Service,
                        Event = intent.Event,
                        Caller = context?.CurrentAsCaller(),
                        FlowContext = context.FlowContext,
                        Parameters = intent.Parameters
                    };

                    var publisher = _eventPublisherProvider.GetPublisher(intent.Service, intent.Event);
                    await publisher.PublishAsync(eventData, default);
                }
            }
        }

        private MethodExecutionState GetMethodExecutionState(
            SaveStateIntent saveStateIntent,
            ITransitionCarrier transitionCarrier,
            ITransitionContext context)
        {
            return new MethodExecutionState
            {
                Service = saveStateIntent.Service,
                Method = saveStateIntent.Method,
                Caller = context.Caller,
                Continuation = transitionCarrier.GetContinuationsAsync(default).Result?.FirstOrDefault(),
                MethodState = saveStateIntent.RoutineState,
                FlowContext = context.FlowContext,
                ContinuationState = (transitionCarrier as TransitionCarrier)?.ContinuationState
            };
        }

        private SerializedMethodContinuationState EncodeContinuationState(
            SaveStateIntent saveStateIntent,
            ITransitionCarrier transitionCarrier,
            ITransitionContext context)
        {
            var executionState = GetMethodExecutionState(saveStateIntent, transitionCarrier, context);

            // TODO: compress
            // TODO: encrypt
            return new SerializedMethodContinuationState
            {
                Format = _defaultSerializer.Format,
                State = _defaultSerializer.SerializeToBytes(executionState)
            };
        }

        private MethodExecutionState DecodeContinuationData(SerializedMethodContinuationState state)
        {
            if (state?.State == null || state.State.Length == 0)
                return null;

            var serializer = _serializeProvder.GetSerializer(state.Format);
            var executionState = serializer.Deserialize<MethodExecutionState>(state.State);
            return executionState;
        }
    }
}
