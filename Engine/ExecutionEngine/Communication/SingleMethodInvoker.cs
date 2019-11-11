using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Transitions;
using Dasync.ExecutionEngine.Utils;
using Dasync.Modeling;

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

            bool preferToRunInPlace = behaviorSettings.RunInPlace && serviceRef.Definition.Type != ServiceType.External;
            bool runInPlace = preferToRunInPlace && (!behaviorSettings.Persistent ||
                communicator.Traits.HasFlag(CommunicationTraits.MessageLockOnPublish));

            var invocationData = InvocationDataUtils.CreateMethodInvocationData(intent,
                _transitionScope.IsActive ? _transitionScope.CurrentMonitor.Context : null);

            var resultValueType = methodRef.Definition.MethodInfo.ReturnType;
            if (resultValueType != typeof(void))
            {
                resultValueType = TaskAccessor.GetTaskResultType(resultValueType);
                if (resultValueType == TaskAccessor.VoidTaskResultType)
                    resultValueType = typeof(void);
            }

            if (runInPlace)
            {
                IMessageHandle messageHandle = null;
                if (behaviorSettings.Persistent)
                {
                    var preferences = new InvocationPreferences
                    {
                        LockMessage = true,
                        ResultValueType = resultValueType
                    };

                    var invocationResult = await communicator.InvokeAsync(invocationData, preferences);
                    if (invocationResult.Outcome == InvocationOutcome.Complete)
                        return invocationResult;
                    messageHandle = invocationResult.MessageHandle;
                }

                try
                {
                    var result = await _localMethodRunner.RunAsync(
                        invocationData,
                        RuntimeCommunicatorMessage.Instance);

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

                    Synchronous = communicator.Traits.HasFlag(CommunicationTraits.SyncReplies),
                    ResultValueType = resultValueType
                };

                return await communicator.InvokeAsync(invocationData, preferences);
            }
        }

        private class RuntimeCommunicatorMessage : ICommunicatorMessage
        {
            public static ICommunicatorMessage Instance = new RuntimeCommunicatorMessage();

            public string CommunicatorType => "_";

            public CommunicationTraits CommunicatorTraits => CommunicationTraits.Volatile;

            public bool? IsRetry => false;

            public string RequestId => null;
        }
    }
}
