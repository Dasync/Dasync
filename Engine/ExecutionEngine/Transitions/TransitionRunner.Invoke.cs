﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Resolvers;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Transitions
{
    public class TransitionContextData : ITransitionContext
    {
        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public string IntentId { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }
    }

    public partial class TransitionRunner
    {
        private IValueContainer GetMethodParameters(IMethodInvocationData data)
        {
            if (data is IMethodSerializedParameters serializedParameters && !string.IsNullOrEmpty(serializedParameters.ContentType))
            {
                return new SerializedValueContainer(
                    serializedParameters.ContentType,
                    serializedParameters.SerializedForm,
                    data, DeserializeMethodParameters);
            }
            return DeserializeMethodParameters(data);
        }

        private IValueContainer DeserializeMethodParameters(string contentType, object serializedForm, object state) =>
            DeserializeMethodParameters((IMethodInvocationData)state);

        private IValueContainer DeserializeMethodParameters(IMethodInvocationData data)
        {
            var serviceReference = _serviceResolver.Resolve(data.Service);
            var methodReference = _methodResolver.Resolve(serviceReference.Definition, data.Method);
            var parameters = methodReference.CreateParametersContainer();
            data.ReadInputParameters(parameters);
            return parameters;
        }

        public async Task<InvokeRoutineResult> RunAsync(IMethodInvocationData data, ICommunicatorMessage message, IMethodContinuationState continuationState)
        {
            var serviceReference = _serviceResolver.Resolve(data.Service);
            var methodReference = _methodResolver.Resolve(serviceReference.Definition, data.Method);

            var behaviorSettings = _communicationSettingsProvider.GetMethodSettings(methodReference.Definition);

            //-------------------------------------------------------------------------------
            // MESSGE DE-DUPLICATION
            //-------------------------------------------------------------------------------

            bool needsDeduplication = behaviorSettings.Deduplicate;
            if (methodReference.Definition.IsQuery)
            {
                // NOTE: you can run queries using message passing (non-volatile),
                // or the volatile method uses a callback instead of re-trying.
                if (message.CommunicatorTraits.HasFlag(CommunicationTraits.Volatile) && data.Continuation == null)
                    needsDeduplication = false;
            }
            if (needsDeduplication &&
                !message.CommunicatorTraits.HasFlag(CommunicationTraits.MessageDeduplication) &&
                (message.IsRetry == true || string.IsNullOrEmpty(message.RequestId)))
            {
                // TODO: if has message de-dup'er, check if a dedup
                // return new InvokeRoutineResult { Outcome = InvocationOutcome.Deduplicated };
            }

            //-------------------------------------------------------------------------------
            // UNIT OF WORK
            //-------------------------------------------------------------------------------

            // TODO: Unit of Work - check if there is cached/stored data
            if (message.IsRetry == true)
            {
                // TODO: If has entities in transitions and they have been already committed,
                // skip method transition and re-try sending commands and events.
            }

            //-------------------------------------------------------------------------------
            // DELEGATION TO RESILIENT COMMUNICATOR
            //-------------------------------------------------------------------------------

            IMessageHandle messageHandle = null;

            if (behaviorSettings.Persistent && message.CommunicatorTraits.HasFlag(CommunicationTraits.Volatile))
            {
                // TODO: check if can poll for result! (think about continuation and sync invocation)

                var preferredCommunicator = _communicatorProvider.GetCommunicator(data.Service, data.Method);
                if (message.CommunicatorType != preferredCommunicator.Type &&
                    !preferredCommunicator.Traits.HasFlag(CommunicationTraits.Volatile))
                {
                    var intent = new ExecuteRoutineIntent
                    {
                        Id = data.IntentId,
                        Service = data.Service,
                        Method = data.Method,
                        Continuation = data.Continuation,
                        Parameters = GetMethodParameters(data)
                    };

                    var preferences = new InvocationPreferences
                    {
                        LockMessage = behaviorSettings.RunInPlace &&
                            preferredCommunicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish)
                    };

                    var context = new TransitionContextData
                    {
                        IntentId = data.IntentId,
                        Service = data.Service,
                        Method = data.Method,
                        Caller = data.Caller,
                        FlowContext = data.FlowContext
                    };

                    var invocationResult = await preferredCommunicator.InvokeAsync(intent, context, continuationState, preferences);

                    if (invocationResult.Outcome == InvocationOutcome.Complete && !string.IsNullOrEmpty(data.IntentId))
                        _routineCompletionSink.OnRoutineCompleted(intent.Service, intent.Method, intent.Id, invocationResult.Result);

                    if (invocationResult.Outcome == InvocationOutcome.Scheduled && invocationResult.MessageHandle != null)
                    {
                        // NOTE: will run synchronously below
                        messageHandle = invocationResult.MessageHandle;
                    }
                    else
                    {
                        return invocationResult;
                    }
                }
            }

            //-------------------------------------------------------------------------------
            // RUN METHOD TRANSITION
            //-------------------------------------------------------------------------------

            try
            {
                var adapter = new TransitionCarrier(data, continuationState);
                var transitionDescriptor = new TransitionDescriptor { Type = TransitionType.InvokeRoutine };
                var result = await RunRoutineAsync(adapter, transitionDescriptor, default);
                if (result.Outcome == InvocationOutcome.Complete && !string.IsNullOrEmpty(data.IntentId))
                    _routineCompletionSink.OnRoutineCompleted(data.Service, data.Method, data.IntentId, result.Result);
                if (messageHandle != null)
                    await messageHandle.Complete();
                return result;
            }
            catch
            {
                messageHandle?.ReleaseLock();
                throw;
            }
        }

        public async Task<ContinueRoutineResult> ContinueAsync(IMethodContinuationData data, ICommunicatorMessage message, IMethodContinuationState continuationState)
        {
            var serviceReference = _serviceResolver.Resolve(data.Service);
            var methodReference = _methodResolver.Resolve(serviceReference.Definition, data.Method);

            var behaviorSettings = _communicationSettingsProvider.GetMethodSettings(methodReference.Definition);

            //-------------------------------------------------------------------------------
            // MESSGE DE-DUPLICATION
            //-------------------------------------------------------------------------------

            if (behaviorSettings.Deduplicate &&
                !message.CommunicatorTraits.HasFlag(CommunicationTraits.MessageDeduplication) &&
                (message.IsRetry == true || string.IsNullOrEmpty(message.RequestId)))
            {
                // TODO: if has message de-dup'er, check if a dedup
                // return new InvokeRoutineResult { Outcome = InvocationOutcome.Deduplicated };
            }

            //-------------------------------------------------------------------------------
            // UNIT OF WORK
            //-------------------------------------------------------------------------------

            // TODO: Unit of Work - check if there is cached/stored data
            if (message.IsRetry == true)
            {
                // TODO: If has entities in transitions and they have been already committed,
                // skip method transition and re-try sending commands and events.
            }

            //-------------------------------------------------------------------------------
            // DELEGATION TO RESILIENT COMMUNICATOR
            //-------------------------------------------------------------------------------

            IMessageHandle messageHandle = null;

            if (behaviorSettings.Persistent && message.CommunicatorTraits.HasFlag(CommunicationTraits.Volatile))
            {
                // TODO: check if can poll for result! (think about continuation and sync invocation)

                var preferredCommunicator = _communicatorProvider.GetCommunicator(data.Service, data.Method);
                if (message.CommunicatorType != preferredCommunicator.Type &&
                    !preferredCommunicator.Traits.HasFlag(CommunicationTraits.Volatile))
                {
                    var intent = new ContinueRoutineIntent
                    {
                        Id = data.IntentId,
                        Service = data.Service,
                        Method = data.Method,
                        TaskId = data.TaskId,
                        Result = data.Result,
                        // TODO:
                        //ContinueAt = data.ContinueAt
                    };

                    var preferences = new InvocationPreferences
                    {
                        LockMessage = behaviorSettings.RunInPlace &&
                            preferredCommunicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish)
                    };

                    var context = new TransitionContextData
                    {
                        IntentId = data.IntentId,
                        Service = data.Service,
                        Method = data.Method,
                        Caller = data.Caller,
                        FlowContext = data.FlowContext
                    };

                    var invocationResult = await preferredCommunicator.ContinueAsync(intent, context, continuationState, preferences);

                    // NOTE: will run synchronously below
                    messageHandle = invocationResult.MessageHandle;
                }
            }

            //-------------------------------------------------------------------------------
            // RUN METHOD TRANSITION
            //-------------------------------------------------------------------------------

            try
            {
                var adapter = new TransitionCarrier(data);

                var routineContinuationData = DecodeContinuationData(continuationState);
                if (routineContinuationData != null)
                {
                    adapter.SetRoutineContinuationData(routineContinuationData);
                }
                else
                {
                    throw new NotImplementedException("TODO: persistence");
                }


                var transitionDescriptor = new TransitionDescriptor { Type = TransitionType.ContinueRoutine };
                var result = await RunRoutineAsync(adapter, transitionDescriptor, default);
                if (result.Outcome == InvocationOutcome.Complete && !string.IsNullOrEmpty(data.Method.IntentId))
                    _routineCompletionSink.OnRoutineCompleted(data.Service, data.Method, data.Method.IntentId, result.Result);
                if (messageHandle != null)
                    await messageHandle.Complete();
                return new ContinueRoutineResult
                {
                };
            }
            catch
            {
                messageHandle?.ReleaseLock();
                throw;
            }
        }
    }
}
