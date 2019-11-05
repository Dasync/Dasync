using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Proxy;
using Dasync.ExecutionEngine.Extensions;
using Dasync.ExecutionEngine.Transitions;
using Dasync.Proxy;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Proxy
{
    public class ProxyMethodExecutor : IProxyMethodExecutor
    {
        private readonly ITransitionScope _transitionScope;
        private readonly IMethodIdProvider _routineMethodIdProvider;
        private readonly IEventIdProvider _eventIdProvider;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly ISingleMethodInvoker _singleMethodInvoker;
        private readonly ISingleEventPublisher _singleEventPublisher;

        public ProxyMethodExecutor(
            ITransitionScope transitionScope,
            IMethodIdProvider routineMethodIdProvider,
            IEventIdProvider eventIdProvider,
            IUniqueIdGenerator numericIdGenerator,
            IRoutineCompletionNotifier routineCompletionNotifier,
            IEventSubscriber eventSubscriber,
            ICommunicationSettingsProvider communicationSettingsProvider,
            IMethodInvokerFactory methodInvokerFactory,
            ISingleMethodInvoker singleMethodInvoker,
            ISingleEventPublisher singleEventPublisher)
        {
            _transitionScope = transitionScope;
            _routineMethodIdProvider = routineMethodIdProvider;
            _eventIdProvider = eventIdProvider;
            _idGenerator = numericIdGenerator;
            _routineCompletionNotifier = routineCompletionNotifier;
            _eventSubscriber = eventSubscriber;
            _communicationSettingsProvider = communicationSettingsProvider;
            _methodInvokerFactory = methodInvokerFactory;
            _singleMethodInvoker = singleMethodInvoker;
            _singleEventPublisher = singleEventPublisher;
        }

        public Task Execute<TParameters>(IProxy proxy, MethodInfo methodInfo, ref TParameters parameters)
            where TParameters : IValueContainer
        {
            var serviceProxyContext = (ServiceProxyContext)proxy.Context;
            var methodDefinition = serviceProxyContext.Definition.FindMethod(methodInfo);

            if (methodDefinition == null || methodDefinition.IsIgnored)
            {
                var invoker = _methodInvokerFactory.Create(methodInfo);
                return invoker.Invoke(proxy, parameters);
            }

            var intent = new ExecuteRoutineIntent
            {
                Id = _idGenerator.NewId(),
                Service = serviceProxyContext.Descriptor.Id,
                Method = _routineMethodIdProvider.GetId(methodInfo),
                Parameters = parameters
            };

            Type taskResultType =
                methodInfo.ReturnType == typeof(void)
                ? TaskAccessor.VoidTaskResultType
                : TaskAccessor.GetTaskResultType(methodInfo.ReturnType);

            var taskState = new RoutineReference
            {
                ServiceId = intent.Service,
                MethodId = intent.Method,
                IntentId = intent.Id
#warning must have id of actual routine for dynamic subscription (subscribe after a routine already scheduled).
            };

            var proxyTask = TaskAccessor.CreateTask(taskState, taskResultType);

            bool invokedByRunningMethod = _transitionScope.IsActive &&
                IsCalledByRoutine(
                    _transitionScope.CurrentMonitor.Context,
                    // Skip 2 stack frames: current method and dynamically-generated proxy.
                    // WARNING! DO NOT PUT 'new StackFrame()' into a helper method!
                    new StackFrame(skipFrames: 2, fNeedFileInfo: false));

            bool ignoreTransaction = !invokedByRunningMethod;

            if (!ignoreTransaction && _transitionScope.IsActive)
            {
                var runningMethodSettings = _communicationSettingsProvider.GetMethodSettings(
                    _transitionScope.CurrentMonitor.Context.MethodRef.Definition);

                if (!runningMethodSettings.Transactional)
                    ignoreTransaction = true;
            }

            if (!ignoreTransaction)
            {
                var methodSettings = _communicationSettingsProvider.GetMethodSettings(methodDefinition);
                if (methodSettings.IgnoreTransaction)
                    ignoreTransaction = true;
            }

            if (ignoreTransaction)
            {
                ExecuteAndAwaitInBackground(intent, proxyTask);
            }
            else
            {
                _transitionScope.CurrentMonitor.RegisterIntent(intent, proxyTask);
            }

            return proxyTask;
        }

        /// <remarks>
        /// Fire and forget mode (async void).
        /// </remarks>
        public async void ExecuteAndAwaitInBackground(ExecuteRoutineIntent intent, Task proxyTask)
        {
            // TODO: exception handling
            var result = await _singleMethodInvoker.InvokeAsync(intent);
            if (result.Outcome == InvocationOutcome.Complete)
            {
                proxyTask.TrySetResult(result.Result);
            }
            else
            {
                var tcs = new TaskCompletionSource<ITaskResult>();
                _routineCompletionNotifier.NotifyOnCompletion(intent.Service, intent.Method, intent.Id, tcs, default);
                var taskResult = await tcs.Task;
                proxyTask.TrySetResult(taskResult);
            }
        }

        public void Subscribe(IProxy proxy, EventInfo @event, Delegate @delegate)
        {
            var eventDesc = new EventDescriptor
            {
                Service = ((ServiceProxyContext)proxy.Context).Descriptor.Id,
                Event = _eventIdProvider.GetId(@event)
            };

            if (@delegate.Target is IProxy subscriberProxy)
            {
                var subscriberProxyContext = (ServiceProxyContext)(subscriberProxy.Context ?? ServiceProxyBuildingContext.CurrentServiceProxyContext);
                var subscriberServiceId = subscriberProxyContext.Descriptor.Id;
                var subscriberMethodId = _routineMethodIdProvider.GetId(@delegate.GetMethodInfo());

                var subscriberDesc = new EventSubscriberDescriptor
                {
                    Service = subscriberServiceId,
                    Method = subscriberMethodId
                };

                _eventSubscriber.Subscribe(eventDesc, subscriberDesc);
            }
            else
            {
                throw new NotSupportedException("At this moment event subscribers must be methods of services.");
            }
        }

        public void Unsubscribe(IProxy proxy, EventInfo @event, Delegate @delegate)
        {
            throw new NotSupportedException("Do you ever need to unsubscribe from a service event?");
        }

        public void RaiseEvent<TParameters>(IProxy proxy, EventInfo @event, ref TParameters parameters)
            where TParameters : IValueContainer
        {
            var serviceProxyContext = (ServiceProxyContext)proxy.Context;

            var intent = new RaiseEventIntent
            {
                Id = _idGenerator.NewId(),
                Service = serviceProxyContext.Descriptor.Id,
                Event = _eventIdProvider.GetId(@event),
                Parameters = parameters
            };

            bool invokedByRunningMethod = _transitionScope.IsActive &&
                IsCalledByRoutine(
                    _transitionScope.CurrentMonitor.Context,
                    // Skip 2 stack frames: current method and dynamically-generated proxy.
                    // WARNING! DO NOT PUT 'new StackFrame()' into a helper method!
                    new StackFrame(skipFrames: 2, fNeedFileInfo: false));

            bool ignoreTransaction = !invokedByRunningMethod;

            if (!ignoreTransaction && _transitionScope.IsActive)
            {
                var runningMethodSettings = _communicationSettingsProvider.GetMethodSettings(
                    _transitionScope.CurrentMonitor.Context.MethodRef.Definition);

                if (!runningMethodSettings.Transactional)
                    ignoreTransaction = true;
            }

            if (!ignoreTransaction)
            {
                var eventDefinition = serviceProxyContext.Definition.FindEvent(@event);
                var eventSettings = _communicationSettingsProvider.GetEventSettings(eventDefinition);
                if (eventSettings.IgnoreTransaction)
                    ignoreTransaction = true;
            }

            if (ignoreTransaction)
            {
                RaiseEventInBackground(intent);
            }
            else
            {
                _transitionScope.CurrentMonitor.RegisterIntent(intent);
            }
        }

        public async void RaiseEventInBackground(RaiseEventIntent intent)
        {
            // TODO: exception handling
            await _singleEventPublisher.PublishAsync(intent);
        }

        private static bool IsCalledByRoutine(TransitionContext context, StackFrame callerStackFrame)
        {
            var callerMethodInfo = callerStackFrame.GetMethod();

            bool isCalledByRoutine;

            if (context.RoutineStateMachine != null)
            {
                isCalledByRoutine = ReferenceEquals(context.RoutineStateMachine.GetType(), callerMethodInfo.DeclaringType);
            }
            else
            {
                isCalledByRoutine = ReferenceEquals(context.RoutineMethod, callerMethodInfo);
            }

            return isCalledByRoutine;
        }
    }
}
