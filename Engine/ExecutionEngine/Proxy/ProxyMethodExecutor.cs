using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
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
        private readonly IUniqueIdGenerator _numericIdGenerator;
        private readonly ITransitionCommitter _transitionCommitter;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;
        private readonly IEventSubscriber _eventSubscriber;

        public ProxyMethodExecutor(
            ITransitionScope transitionScope,
            IMethodIdProvider routineMethodIdProvider,
            IEventIdProvider eventIdProvider,
            IUniqueIdGenerator numericIdGenerator,
            ITransitionCommitter transitionCommitter,
            IRoutineCompletionNotifier routineCompletionNotifier,
            IEventSubscriber eventSubscriber)
        {
            _transitionScope = transitionScope;
            _routineMethodIdProvider = routineMethodIdProvider;
            _eventIdProvider = eventIdProvider;
            _numericIdGenerator = numericIdGenerator;
            _transitionCommitter = transitionCommitter;
            _routineCompletionNotifier = routineCompletionNotifier;
            _eventSubscriber = eventSubscriber;
        }

        public Task Execute<TParameters>(IProxy proxy, MethodInfo methodInfo, ref TParameters parameters)
            where TParameters : IValueContainer
        {
            var serviceProxyContext = (ServiceProxyContext)proxy.Context;

            var intent = new ExecuteRoutineIntent
            {
                Id = _numericIdGenerator.NewId(),
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
                IntentId = intent.Id
#warning must have id of actual routine for dynamic subscription (subscribe after a routine already scheduled).
            };

            var proxyTask = TaskAccessor.CreateTask(taskState, taskResultType);

            bool executeInline = !_transitionScope.IsActive || !IsCalledByRoutine(
                    _transitionScope.CurrentMonitor.Context,
                    // Skip 2 stack frames: current method and dynamically-generated proxy.
                    // WARNING! DO NOT PUT 'new StackFrame()' into a helper method!
                    new StackFrame(skipFrames: 2, fNeedFileInfo: false));

            if (executeInline)
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
            // Tell platform to track the completion.
            // Do it before commit as a routine may complete immediately.

            var tcs = new TaskCompletionSource<TaskResult>();
            _routineCompletionNotifier.NotifyCompletion(intent.ServiceId, intent.MethodId, intent.Id, tcs, default);

            // Commit intent

            var options = new TransitionCommitOptions
            {
                // Set the hint about the synchronous call mode.
                NotifyOnRoutineCompletion = true
            };

            var actions = new ScheduledActions
            {
                ExecuteRoutineIntents = new List<ExecuteRoutineIntent>
                {
                    intent
                }
            };
            await _transitionCommitter.CommitAsync(actions, transitionCarrier: null, options: options, ct: default);

            // Await for completion and set the result.

            var routineResult = await tcs.Task;
            proxyTask.TrySetResult(routineResult);
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
                throw new NotSupportedException("At this moment event subscribers must be routines of services.");
            }
        }

        public void Unsubscribe(IProxy proxy, EventInfo @event, Delegate @delegate)
        {
            throw new NotSupportedException("Do you ever need to unsubscribe from an event?");
        }

        public void RaiseEvent<TParameters>(IProxy proxy, EventInfo @event, ref TParameters parameters)
            where TParameters : IValueContainer
        {
            var serviceProxyContext = (ServiceProxyContext)proxy.Context;

            var intent = new RaiseEventIntent
            {
                Id = _numericIdGenerator.NewId(),
                Service = serviceProxyContext.Descriptor.Id,
                Event = _eventIdProvider.GetId(@event),
                Parameters = parameters
            };

            bool executeInline = !_transitionScope.IsActive || !IsCalledByRoutine(
                    _transitionScope.CurrentMonitor.Context,
                    // Skip 2 stack frames: current method and dynamically-generated proxy.
                    // WARNING! DO NOT PUT 'new StackFrame()' into a helper method!
                    new StackFrame(skipFrames: 2, fNeedFileInfo: false));

            if (executeInline)
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
            var actions = new ScheduledActions
            {
                RaiseEventIntents = new List<RaiseEventIntent>
                {
                    intent
                }
            };

            var options = new TransitionCommitOptions();

            await _transitionCommitter.CommitAsync(actions, transitionCarrier: null, options: options, ct: default);
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
