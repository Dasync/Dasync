using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Transitions;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Communication
{
    /// <summary>
    /// Invokes (and petentially runs in place) a single method outside a transaction.
    /// </summary>
    public class SingleMethodInvoker : ISingleMethodInvoker
    {
        private readonly ITransitionScope _transitionScope;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
        private readonly ICommunicatorProvider _communicatorProvider;
        private readonly ILocalMethodRunner _localMethodRunner;

        public SingleMethodInvoker(
            ITransitionScope transitionScope,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            ICommunicationSettingsProvider communicationSettingsProvider,
            ICommunicatorProvider communicatorProvider,
            ILocalMethodRunner localMethodRunner)
        {
            _transitionScope = transitionScope;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _communicationSettingsProvider = communicationSettingsProvider;
            _communicatorProvider = communicatorProvider;
            _localMethodRunner = localMethodRunner;
        }

        public async Task<InvokeRoutineResult> InvokeAsync(ExecuteRoutineIntent intent)
        {
            var serviceRef = _serviceResolver.Resolve(intent.Service);
            var methodRef = _methodResolver.Resolve(serviceRef.Definition, intent.Method);

            var behaviorSettings = _communicationSettingsProvider.GetMethodSettings(methodRef.Definition);

            ICommunicator communicator = _communicatorProvider.GetCommunicator(serviceRef.Id, methodRef.Id);

            bool preferToRunInPlace = behaviorSettings.RunInPlace && serviceRef.Definition.Type != Modeling.ServiceType.External;
            bool runInPlace = preferToRunInPlace && (!behaviorSettings.Persistent ||
                communicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish));

            ITransitionContext transitionContext;
            if (_transitionScope.IsActive)
            {
                transitionContext = _transitionScope.CurrentMonitor.Context;
            }
            else
            {
                transitionContext = new TransitionContextData
                {
                    // TODO: hints can be used to set unique intent/request ID
                };
            }

            if (runInPlace)
            {
                IMessageHandle messageHandle = null;
                if (behaviorSettings.Persistent)
                {
                    var preferences = new InvocationPreferences { LockMessage = true };

                    var invocationResult = await communicator.InvokeAsync(intent, transitionContext, null, preferences);
                    if (invocationResult.Outcome == InvocationOutcome.Complete)
                        return invocationResult;
                    messageHandle = invocationResult.MessageHandle;
                }

                try
                {
                    var invocationData = new MethodInvocationData
                    {
                        IntentId = intent.Id,
                        Service = intent.Service,
                        Method = intent.Method,
                        Parameters = intent.Parameters
                    };

                    if (_transitionScope.IsActive)
                    {
                        invocationData.FlowContext = transitionContext.FlowContext;
                        invocationData.Caller = transitionContext.CurrentAsCaller();
                    }

                    var result = await _localMethodRunner.RunAsync(invocationData, message: invocationData, continuationState: null);
                    if (messageHandle != null)
                        await messageHandle.Complete();
                    return result;
                }
                catch (Exception ex) // TODO: infra exceptions? should not be there, right?
                {
                    if (messageHandle != null)
                        messageHandle.ReleaseLock();

                    return new InvokeRoutineResult
                    {
                        Outcome = InvocationOutcome.Scheduled,
                        MessageHandle = messageHandle
                    };
                }
            }
            else
            {
                var preferences = new InvocationPreferences
                {
                    // TODO: check this option
                    //LockMessage = behaviorSettings.Resilient && communicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish),
                    
                    Synchronous = communicator.Traits.HasFlag(CommunicationTraits.SyncReplies)
                };

                return await communicator.InvokeAsync(intent, transitionContext, null, preferences);
            }
        }

        private class MethodInvocationData : IMethodInvocationData, ICommunicatorMessage
        {
            public ServiceId Service { get; set; }

            public MethodId Method { get; set; }

            public ContinuationDescriptor Continuation => null;

            public string IntentId { get; set; }

            public CallerDescriptor Caller { get; set; }

            public Dictionary<string, string> FlowContext { get; set; }

            public string CommunicatorType => "_";

            public CommunicationTraits CommunicatorTraits => CommunicationTraits.Volatile;

            public bool? IsRetry => false;

            public string RequestId => null;

            public IValueContainer Parameters { get; set; }

            public Task ReadInputParameters(IValueContainer target)
            {
                Parameters.CopyTo(target);
                return Task.CompletedTask;
            }
        }
    }
}
