using System;
using System.Collections.Generic;
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
        private readonly IRoutineMethodIdProvider _routineMethodIdProvider;
        private readonly IEventIdProvider _eventIdProvider;
        private readonly INumericIdGenerator _numericIdGenerator;
        private readonly ITransitionCommitter _transitionCommitter;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;
        private readonly IEventSubscriber _eventSubscriber;

        public ProxyMethodExecutor(
            ITransitionScope transitionScope,
            IRoutineMethodIdProvider routineMethodIdProvider,
            IEventIdProvider eventIdProvider,
            INumericIdGenerator numericIdGenerator,
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
                ServiceId = serviceProxyContext.Service.Id,
                MethodId = _routineMethodIdProvider.GetId(methodInfo),
                Parameters = parameters
            };

            var taskResultType =
                // Dispose() does not return a task, and is the only exception.
#warning check if it's really IDisposable.Dispose
                methodInfo.ReturnType == typeof(void)
                ? TaskAccessor.VoidTaskResultType
                : TaskAccessor.GetTaskResultType(methodInfo.ReturnType);

            var taskState = new RoutineReference
            {
                IntentId = intent.Id
#warning must have id of actual routine for dynamic subscription (subscribe after a routine already scheduled).
            };

            var proxyTask = TaskAccessor.CreateTask(taskState, taskResultType);

            if (_transitionScope.IsActive)
            {
                _transitionScope.CurrentMonitor.RegisterIntent(intent, proxyTask);
            }
            else
            {
                ExecuteAndAwaitInBackground(intent, proxyTask);
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
            _routineCompletionNotifier.NotifyCompletion(intent.Id, tcs);

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
                ServiceId = ((ServiceProxyContext)proxy.Context).Service.Id,
                EventId = _eventIdProvider.GetId(@event)
            };

            if (@delegate.Target is IProxy subscriberProxy)
            {
                var subscriberProxyContext = (ServiceProxyContext)(subscriberProxy.Context ?? ServiceProxyBuildingContext.CurrentServiceProxyContext);
                var subscriberServiceId = subscriberProxyContext.Service.Id;
                var subscriberMethodId = _routineMethodIdProvider.GetId(@delegate.GetMethodInfo());

                var subscriberDesc = new EventSubscriberDescriptor
                {
                    ServiceId = subscriberServiceId,
                    MethodId = subscriberMethodId
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
                ServiceId = serviceProxyContext.Service.Id,
                EventId = _eventIdProvider.GetId(@event),
                Parameters = parameters
            };

            if (_transitionScope.IsActive)
            {
                _transitionScope.CurrentMonitor.RegisterIntent(intent);
            }
            else
            {
                RaiseEventInBackground(intent);
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
    }
}
