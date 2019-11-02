using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Resolvers;

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
        public async Task<InvokeRoutineResult> RunAsync(MethodInvocationData data, ICommunicatorMessage message, SerializedMethodContinuationState continuationState)
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
                (message.IsRetry != false || string.IsNullOrEmpty(message.RequestId)))
            {
                // TODO: if has message de-dup'er, check if a dedup
                // return new InvokeRoutineResult { Outcome = InvocationOutcome.Deduplicated };
            }

            //-------------------------------------------------------------------------------
            // UNIT OF WORK
            //-------------------------------------------------------------------------------

            // TODO: Unit of Work - check if there is cached/stored data
            if (message.IsRetry != false)
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
                    var resultValueType = methodReference.Definition.MethodInfo.ReturnType;
                    if (resultValueType != typeof(void))
                    {
                        resultValueType = TaskAccessor.GetTaskResultType(resultValueType);
                        if (resultValueType == TaskAccessor.VoidTaskResultType)
                            resultValueType = typeof(void);
                    }

                    var preferences = new InvocationPreferences
                    {
                        LockMessage = behaviorSettings.RunInPlace &&
                            preferredCommunicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish),
                        ResultValueType = resultValueType
                    };

                    var invocationResult = await preferredCommunicator.InvokeAsync(data, continuationState, preferences);

                    if (invocationResult.Outcome == InvocationOutcome.Complete && !string.IsNullOrEmpty(data.IntentId))
                        _routineCompletionSink.OnRoutineCompleted(data.Service, data.Method, data.IntentId, invocationResult.Result);

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
                var adapter = new TransitionCarrier(data, continuationState, _valueContainerCopier);
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

        public async Task<ContinueRoutineResult> ContinueAsync(MethodContinuationData data, ICommunicatorMessage message, SerializedMethodContinuationState continuationState)
        {
            var serviceReference = _serviceResolver.Resolve(data.Service);
            var methodReference = _methodResolver.Resolve(serviceReference.Definition, data.Method);

            var behaviorSettings = _communicationSettingsProvider.GetMethodSettings(methodReference.Definition);

            //-------------------------------------------------------------------------------
            // MESSGE DE-DUPLICATION
            //-------------------------------------------------------------------------------

            if (behaviorSettings.Deduplicate &&
                !message.CommunicatorTraits.HasFlag(CommunicationTraits.MessageDeduplication) &&
                (message.IsRetry != false || string.IsNullOrEmpty(message.RequestId)))
            {
                // TODO: if has message de-dup'er, check if a dedup
                // return new InvokeRoutineResult { Outcome = InvocationOutcome.Deduplicated };
            }

            //-------------------------------------------------------------------------------
            // UNIT OF WORK
            //-------------------------------------------------------------------------------

            // TODO: Unit of Work - check if there is cached/stored data
            if (message.IsRetry != false)
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
                    var preferences = new InvocationPreferences
                    {
                        LockMessage = behaviorSettings.RunInPlace &&
                            preferredCommunicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish)
                    };

                    var invocationResult = await preferredCommunicator.ContinueAsync(data, continuationState, preferences);

                    if (!preferences.LockMessage || invocationResult.MessageHandle == null)
                    {
                        return new ContinueRoutineResult { };
                    }

                    // NOTE: will run synchronously below
                    messageHandle = invocationResult.MessageHandle;
                }
            }

            //-------------------------------------------------------------------------------
            // RUN METHOD TRANSITION
            //-------------------------------------------------------------------------------

            try
            {
            @TryRun:
                var adapter = new TransitionCarrier(data);

                MethodExecutionState methodState = DecodeContinuationData(continuationState);
                if (methodState == null)
                {
                    var stateStorage = _methodStateStorageProvider.GetStorage(data.Service, data.Method, returnNullIfNotFound: true);
                    if (stateStorage == null)
                        throw new InvalidOperationException($"Cannot resume method '{data.Service}'.{data.Method} due to absence of persistence mechanism.");

                    methodState = await stateStorage.ReadStateAsync(data.Service, data.Method, default);
                }

                adapter.SetMethodExecutionState(methodState, _valueContainerCopier);

                InvokeRoutineResult result;
                try
                {
                    var transitionDescriptor = new TransitionDescriptor { Type = TransitionType.ContinueRoutine };
                    result = await RunRoutineAsync(adapter, transitionDescriptor, default);
                }
                catch (ConcurrentTransitionException)
                {
                    goto TryRun;
                }

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
