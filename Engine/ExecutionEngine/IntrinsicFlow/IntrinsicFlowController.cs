using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.AsyncStateMachine;
using Dasync.EETypes;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Proxy;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.Transitions;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    public interface IIntrinsicFlowController
    {
        void OnRoutineStart(ITransitionMonitor monitor);

        void OnRoutineTransitionComplete(ITransitionMonitor monitor);

        bool TryHandlePreInvoke(
            Delegate @delegate,
            object continuationObject,
            ITransitionMonitor monitor);

        void TryHandlePostInvoke(
            Delegate @delegate,
            object continuationObject,
            ITransitionMonitor monitor);

        void TryHandlePostMoveNext(
            ITransitionMonitor monitor);

        void HandleWhenAllAsDetectedContinuation(
            Task whenAllTask, Task[] awaitedTasks,
            ExecuteRoutineIntent awaitedRoutineIntent,
            ITransitionMonitor monitor);
    }

    public class IntrinsicFlowController : IIntrinsicFlowController
    {
        private readonly ITaskContinuationClassifier _taskContinuationClassifier;
        private readonly IAsyncStateMachineMetadataProvider _asyncStateMachineMetadataProvider;
        private readonly INumericIdGenerator _numericIdGenerator;
        private readonly IRoutineMethodIdProvider _routineMethodIdProvider;

        public IntrinsicFlowController(
            ITaskContinuationClassifier taskContinuationClassifier,
            IAsyncStateMachineMetadataProvider asyncStateMachineMetadataProvider,
            INumericIdGenerator numericIdGenerator,
            IRoutineMethodIdProvider routineMethodIdProvider)
        {
            _taskContinuationClassifier = taskContinuationClassifier;
            _asyncStateMachineMetadataProvider = asyncStateMachineMetadataProvider;
            _numericIdGenerator = numericIdGenerator;
            _routineMethodIdProvider = routineMethodIdProvider;
        }

        public void OnRoutineStart(ITransitionMonitor monitor)
        {
            var syncContext = new InterceptingSynchronizationContext(
                SynchronizationContext.Current, this, monitor);

            SynchronizationContext.SetSynchronizationContext(syncContext);
        }

        public void OnRoutineTransitionComplete(ITransitionMonitor monitor)
        {
#warning THIS DOES NOT WORK! Because it's called from a synchronization context, where execution context is different. Need to call it e.g. from the transition runner, but that's ugly.

#warning check if it's the same context?
            var syncContext = SynchronizationContext.Current;
            if (syncContext != null)
            {
                var routineSyncContext = (InterceptingSynchronizationContext)syncContext;
                SynchronizationContext.SetSynchronizationContext(routineSyncContext.InnerContext);
            }
        }

        public bool TryHandlePreInvoke(
            Delegate @delegate,
            object continuationObject,
            ITransitionMonitor monitor)
        {
            if (@delegate.GetMethodInfo().DeclaringType == typeof(YieldAwaitable.YieldAwaiter) &&
                monitor.Context.RoutineStateMachine != null)
            {
                var continuationInfo = _taskContinuationClassifier.GetContinuationInfo(continuationObject);
                if (continuationInfo.Type == TaskContinuationType.AsyncStateMachine &&
                    ReferenceEquals(monitor.Context.RoutineStateMachine, continuationInfo.Target))
                {
                    monitor.OnCheckpointIntent();
                    return true;
                }
            }

            return false;
        }

        public void TryHandlePostInvoke(
            Delegate @delegate,
            object continuationObject,
            ITransitionMonitor monitor)
        {
#warning This is heavy weight. Analyzing just the state machine for the delay promise is must be good enough.
            var continuationInfo = _taskContinuationClassifier.GetContinuationInfo(continuationObject);
            if (continuationInfo.Type == TaskContinuationType.AsyncStateMachine &&
                ReferenceEquals(monitor.Context.RoutineStateMachine, continuationInfo.Target))
            {
                TryHandlePostMoveNext(monitor);
            }
        }

        public void TryHandlePostMoveNext(ITransitionMonitor monitor)
        {
            if (TryFindDelayPromise(monitor.Context.RoutineStateMachine, out var delayPromise)
                && DelayPromiseAccessor.TryGetTimerStartAndDelay(
                    delayPromise, out var timerStart, out var timerDelay)
#warning Needs a configuration setting for the minimum delay. If a delay is too short, it might be too expensive to try to save the state of a routine. Also, add ability to opt in/out auto save?
                //&& timerDelay > TimeSpan.FromSeconds(5)
                && DelayPromiseAccessor.TryCancelTimer(delayPromise))
            {
                delayPromise.ResetContinuation();
                delayPromise.TrySetResult(null);
                monitor.OnCheckpointIntent(resumeTime: timerStart + timerDelay);
            }
        }

        private bool TryFindDelayPromise(
            IAsyncStateMachine stateMachine,
            out Task delayPromise)
        {
#warning such analysis can be optimizer by pre-compiling the code per state machine type
            var metadata = _asyncStateMachineMetadataProvider.GetMetadata(stateMachine.GetType());

            foreach (var variable in metadata.LocalVariables)
            {
                if (TaskAwaiterUtils.IsAwaiterType(variable.FieldInfo.FieldType))
                {
                    var awaiter = variable.FieldInfo.GetValue(stateMachine);
                    var task = TaskAwaiterUtils.GetTask(awaiter);
                    if (task != null && DelayPromiseAccessor.IsDelayPromise(task))
                    {
                        delayPromise = task;
                        return true;
                    }
                }
            }

            delayPromise = null;
            return false;
        }

        public void HandleWhenAllAsDetectedContinuation(
            Task whenAllTask, Task[] awaitedTasks,
            ExecuteRoutineIntent awaitedRoutineIntent,
            ITransitionMonitor monitor)
        {
            ExecuteRoutineIntent executeWhenAllIntent;

            lock (whenAllTask)
            {
                var state = whenAllTask.AsyncState as WhenAllProxyTaskState;
                if (state == null)
                {
                    var whenAllIntentId = _numericIdGenerator.NewId();

                    Type itemType = null;
                    var resultType = whenAllTask.GetResultType();
                    if (resultType.IsArray)
                        itemType = resultType.GetElementType();

                    executeWhenAllIntent = new ExecuteRoutineIntent
                    {
                        Id = whenAllIntentId,

                        ServiceId = new ServiceId
                        {
                            ServiceName = nameof(IntrinsicRoutines)
                        },

                        MethodId = _routineMethodIdProvider.GetId(
                            IntrinsicRoutines.WhenAllMethodInfo),

                        Parameters = new WhenAllInputParameters
                        {
                            tasks = awaitedTasks,
                            intents = new ExecuteRoutineIntent[awaitedTasks.Length],
                            // The task result must be an Array, e.g. string[]
                            itemType = itemType
                        }
                    };

                    state = new WhenAllProxyTaskState
                    {
                        IntentId = whenAllIntentId,
                        ExecuteWhenAllIntent = executeWhenAllIntent
                    };
                    whenAllTask.SetAsyncState(state);

                    monitor.RegisterIntent(executeWhenAllIntent, whenAllTask);
                }
                else
                {
                    executeWhenAllIntent = state.ExecuteWhenAllIntent;
                }
            }

            var index = -1;
            for (var i = 0; i < awaitedTasks.Length; i++)
            {
                if (awaitedTasks[i].AsyncState is ProxyTaskState state
                    && state.IntentId == awaitedRoutineIntent.Id)
                {
                    index = i;
                    break;
                }
            }
            var parameters = (WhenAllInputParameters)executeWhenAllIntent.Parameters;
            parameters.intents[index] = awaitedRoutineIntent;

            monitor.Context.ScheduledActions.ExecuteRoutineIntents.Remove(awaitedRoutineIntent);

            if (parameters.intents.All(i => i != null))
            {
                whenAllTask.SetAsyncState(new ProxyTaskState
                {
                    IntentId = executeWhenAllIntent.Id
                });
            }
        }
    }

    public class WhenAllProxyTaskState : ProxyTaskState
    {
        public ExecuteRoutineIntent ExecuteWhenAllIntent;
    }
}
