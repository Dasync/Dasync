using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
        private readonly INumericIdGenerator _numericIdGenerator;
        private readonly ITransitionCommitter _transitionCommitter;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;

        public ProxyMethodExecutor(
            ITransitionScope transitionScope,
            IRoutineMethodIdProvider routineMethodIdProvider,
            INumericIdGenerator numericIdGenerator,
            ITransitionCommitter transitionCommitter,
            IRoutineCompletionNotifier routineCompletionNotifier)
        {
            _transitionScope = transitionScope;
            _routineMethodIdProvider = routineMethodIdProvider;
            _numericIdGenerator = numericIdGenerator;
            _transitionCommitter = transitionCommitter;
            _routineCompletionNotifier = routineCompletionNotifier;
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

            var taskState = new ProxyTaskState
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
            // Commit intent

            // Set the hint about the synchronous call mode.
            intent.NotifyOnCompletion = true;
            var actions = new ScheduledActions
            {
                ExecuteRoutineIntents = new List<ExecuteRoutineIntent>
                {
                    intent
                }
            };
            await _transitionCommitter.CommitAsync(actions, transitionCarrier: null, ct: default(CancellationToken));

            // Tell platform to track the completion.

            var tcs = new TaskCompletionSource<TaskResult>();
            _routineCompletionNotifier.NotifyCompletion(intent.Id, tcs);

            // Await for completion and set the result.

            var routineResult = await tcs.Task;
            proxyTask.TrySetResult(routineResult);
        }
    }
}
