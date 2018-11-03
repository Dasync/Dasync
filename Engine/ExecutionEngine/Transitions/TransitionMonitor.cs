using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.AsyncStateMachine;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.IntrinsicFlow;

namespace Dasync.ExecutionEngine.Transitions
{
    public interface ITransitionMonitorFactory
    {
        ITransitionMonitor Create(TransitionContext context);
    }

    public class TransitionMonitorFactory : ITransitionMonitorFactory
    {
        private readonly ITaskContinuationTracker _taskContinuationTracker;
        private readonly ITaskContinuationClassifier _taskContinuationClassifier;
        private readonly IIntrinsicFlowController _intrinsicFlowController;
        private readonly IAsyncStateMachineMetadataProvider _asyncStateMachineMetadataProvider;

        public TransitionMonitorFactory(
            ITaskContinuationTracker taskContinuationTracker,
            ITaskContinuationClassifier taskContinuationClassifier,
            IIntrinsicFlowController intrinsicFlowController,
            IAsyncStateMachineMetadataProvider asyncStateMachineMetadataProvider)
        {
            _taskContinuationTracker = taskContinuationTracker;
            _taskContinuationClassifier = taskContinuationClassifier;
            _intrinsicFlowController = intrinsicFlowController;
            _asyncStateMachineMetadataProvider = asyncStateMachineMetadataProvider;
        }

        public ITransitionMonitor Create(TransitionContext context)
        {
            return new TransitionMonitor(
                context,
                _taskContinuationTracker,
                _taskContinuationClassifier,
                _intrinsicFlowController,
                _asyncStateMachineMetadataProvider);
        }
    }

    /// <summary>
    /// Responsible for monitoring various intents (<see cref="ScheduledActions"/>),
    /// process them, and delegate execution to <see cref="ITransitionCommitter"/>
    /// when logical end is reached.
    /// </summary>
    public interface ITransitionMonitor
    {
        TransitionContext Context { get; }

        void OnRoutineStart(
            ServiceId serviceId,
            RoutineDescriptor routineDesc,
            object serviceInstance,
            MethodInfo routineMethod,
            IAsyncStateMachine routineStateMachine);

        Task<ScheduledActions> TrackRoutineCompletion(Task routineCompletionTask);

        void RegisterIntent(ExecuteRoutineIntent intent, Task proxyTask);

        void RegisterIntent(RaiseEventIntent intent);

        /// <summary>
        /// Save the state of current routine, then continue executing it.
        /// </summary>
        /// <remarks>
        /// Translated from <see cref="Task.Yield"/> and <see cref="Task.Delay(TimeSpan)"/>.
        /// </remarks>
        void OnCheckpointIntent(DateTime? resumeTime = null);

        void SaveStateWithoutResume();
    }

    public class TransitionMonitor : ITransitionMonitor
    {
        private readonly ITaskContinuationTracker _taskContinuationTracker;
        private readonly ITaskContinuationClassifier _taskContinuationClassifier;
        private readonly IIntrinsicFlowController _intrinsicFlowController;
        private readonly OnTaskContinuationSetCallback _onRoutineContinuationSetCallback;
        private readonly IAsyncStateMachineMetadataProvider _asyncStateMachineMetadataProvider;

        public TransitionMonitor(
            TransitionContext context,
            ITaskContinuationTracker taskContinuationTracker,
            ITaskContinuationClassifier taskContinuationClassifier,
            IIntrinsicFlowController intrinsicFlowController,
            IAsyncStateMachineMetadataProvider asyncStateMachineMetadataProvider)
        {
            Context = context;
            _taskContinuationTracker = taskContinuationTracker;
            _taskContinuationClassifier = taskContinuationClassifier;
            _intrinsicFlowController = intrinsicFlowController;
            _asyncStateMachineMetadataProvider = asyncStateMachineMetadataProvider;
            _onRoutineContinuationSetCallback = OnRoutineContinuationSet;
        }

        public TransitionContext Context { get; }

        public void OnRoutineStart(
            ServiceId serviceId,
            RoutineDescriptor routineDesc,
            object serviceInstance,
            MethodInfo routineMethod,
            IAsyncStateMachine routineStateMachine)
        {
            Context.ServiceId = serviceId;
            Context.RoutineDescriptor = routineDesc;
            Context.ServiceInstance = serviceInstance;
            Context.RoutineMethod = routineMethod;
            Context.RoutineStateMachine = routineStateMachine;

            _intrinsicFlowController.OnRoutineStart(this);
        }

        public Task<ScheduledActions> TrackRoutineCompletion(Task routineCompletionTask)
        {
            Context.RoutineResultTask = routineCompletionTask;

            if (!routineCompletionTask.IsCompleted && Context.RoutineStateMachine != null)
                _intrinsicFlowController.TryHandlePostMoveNext(this);

            if (routineCompletionTask.IsCompleted)
            {
                OnRoutineCompleted(routineCompletionTask);
            }
            else if (!Context.TransitionCompleteTask.IsCompleted)
            {
                routineCompletionTask.ContinueWith(OnRoutineCompleted);
            }

            return Context.TransitionCompleteTask;
        }

        public void RegisterIntent(ExecuteRoutineIntent intent, Task proxyTask)
        {
            var transitionContext = Context;
            var actions = transitionContext.ScheduledActions;
            if (actions.ExecuteRoutineIntents == null)
                actions.ExecuteRoutineIntents = new List<ExecuteRoutineIntent>();
            actions.ExecuteRoutineIntents.Add(intent);
            _taskContinuationTracker.StartTracking(proxyTask, _onRoutineContinuationSetCallback, intent);
        }

        public void RegisterIntent(RaiseEventIntent intent)
        {
            var transitionContext = Context;
            var actions = transitionContext.ScheduledActions;
            if (actions.RaiseEventIntents == null)
                actions.RaiseEventIntents = new List<RaiseEventIntent>();
            actions.RaiseEventIntents.Add(intent);
        }

        public void OnCheckpointIntent(DateTime? resumeTime = null)
        {
            var transitionContext = Context;
            var actions = transitionContext.ScheduledActions;
            actions.SaveRoutineState = true;
            actions.ResumeRoutineIntent = new ContinueRoutineIntent
            {
                Continuation = new ContinuationDescriptor
                {
                    ServiceId = transitionContext.ServiceId,
                    Routine = transitionContext.RoutineDescriptor,
                    ContinueAt = resumeTime
                }
            };
            CompleteTransition();
        }

        public void SaveStateWithoutResume()
        {
            var transitionContext = Context;
            var actions = transitionContext.ScheduledActions;
            actions.SaveRoutineState = true;
            CompleteTransition();
        }

        private void OnRoutineContinuationSet(Task routineCompletionTask, object continuationObject, object userData)
        {
            var transitionContext = Context;
            var continuationInfo = _taskContinuationClassifier.GetContinuationInfo(continuationObject);

            if (continuationInfo.Type == TaskContinuationType.AsyncStateMachine &&
                AreTheSameStateMachines(
                    transitionContext.RoutineStateMachine,
                    (IAsyncStateMachine)continuationInfo.Target))
            {
                transitionContext.ScheduledActions.SaveRoutineState = true;

                var intent = (ExecuteRoutineIntent)userData;
                intent.Continuation = new ContinuationDescriptor
                {
                    ServiceId = transitionContext.ServiceId,
                    Routine = transitionContext.RoutineDescriptor
                };

                CompleteTransition();
            }
            else if (continuationInfo.Type == TaskContinuationType.WhenAll)
            {
                var awaitedRoutineIntent = (ExecuteRoutineIntent)userData;
                _intrinsicFlowController.HandleWhenAllAsDetectedContinuation(
                    whenAllTask: (Task)continuationInfo.Target,
                    awaitedTasks: (Task[])continuationInfo.Items,
                    awaitedRoutineIntent: awaitedRoutineIntent,
                    monitor: this);
            }
            else
            {
                throw new NotImplementedException(
                    $"Need to await for the result in process for:" +
                    $" {continuationInfo.Type} / {continuationObject.GetType()}");
            }
        }

        private void OnRoutineCompleted(Task routineCompletionTask)
        {
            var transitionContext = Context;
            transitionContext.ScheduledActions.SaveRoutineState = true;
            CompleteTransition();
        }

        private void CompleteTransition()
        {
            _intrinsicFlowController.OnRoutineTransitionComplete(this);
            Context.CompleteTransition();
        }

        private bool AreTheSameStateMachines(IAsyncStateMachine asm1, IAsyncStateMachine asm2)
        {
            if (ReferenceEquals(asm1, asm2))
                return true;

            if (asm1.GetType() != asm2.GetType())
                return false;

            // WARNING!
            // In RELEASE mode a state machine is compiled into a values type, not reference type!
            // The builder is also is a value type, so the only way to say exactly is to compare
            // the completion Tasks, which are always reference types.
            if (asm1.GetType().IsValueType())
            {
#warning Optimize this code, don't inject IAsyncStateMachineMetadataProvider into this class.
                var metadata = _asyncStateMachineMetadataProvider.GetMetadata(asm1.GetType());
                var builder1 = metadata.Builder.FieldInfo.GetValue(asm1);
                var builder2 = metadata.Builder.FieldInfo.GetValue(asm2);
                var taskField = metadata.Builder.FieldInfo.FieldType.GetProperty("Task");
                var completionTask1 = taskField.GetValue(builder1);
                var completionTask2 = taskField.GetValue(builder2);
                return ReferenceEquals(completionTask1, completionTask2);
            }

            return false;
        }
    }
}
