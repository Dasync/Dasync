﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.AsyncStateMachine;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Resolvers;
using Dasync.EETypes.Triggers;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.Extensions;
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
    /// Responsible for monitoring various intents (<see cref="ScheduledActions"/>).
    /// </summary>
    public interface ITransitionMonitor
    {
        TransitionContext Context { get; }

        void OnRoutineStart(
            IServiceReference serviceRef,
            IMethodReference methodRef,
            PersistedMethodId methodId,
            object serviceInstance,
            IAsyncStateMachine routineStateMachine,
            CallerDescriptor caller);

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

        void AwaitTrigger(TriggerReference triggerReference);

        void ActivateTrigger(Task trigger, TriggerReference triggerReference);
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
            IServiceReference serviceRef,
            IMethodReference methodRef,
            PersistedMethodId methodId,
            object serviceInstance,
            IAsyncStateMachine routineStateMachine,
            CallerDescriptor caller)
        {
            Context.ServiceRef = serviceRef;
            Context.MethodRef = methodRef;
            Context.MethodId = methodId;
            Context.ServiceInstance = serviceInstance;
            Context.RoutineStateMachine = routineStateMachine;
            Context.Caller = caller;

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
                routineCompletionTask.ContinueWith(OnRoutineCompleted, TaskContinuationOptions.ExecuteSynchronously);
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
                Service = transitionContext.ServiceRef.Id,
                Method = transitionContext.MethodId,
                ContinueAt = resumeTime
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

        public void AwaitTrigger(TriggerReference triggerReference)
        {
            Context.ScheduledActions.SaveRoutineState = true;
            if (Context.ScheduledActions.SubscribeToTriggerIntents == null)
                Context.ScheduledActions.SubscribeToTriggerIntents = new List<SubscribeToTriggerIntent>();
            Context.ScheduledActions.SubscribeToTriggerIntents.Add(new SubscribeToTriggerIntent
            {
                TriggerId = triggerReference.Id,
                Continuation = new ContinuationDescriptor
                {
                    Service = Context.ServiceRef.Id,
                    Method = Context.MethodId,
                    TaskId = triggerReference.Id
                }
            });
            CompleteTransition();
        }

        public void ActivateTrigger(Task trigger, TriggerReference triggerReference)
        {
            if (Context.ScheduledActions.ActivateTriggerIntents == null)
                Context.ScheduledActions.ActivateTriggerIntents = new List<ActivateTriggerIntent>();

            Context.ScheduledActions.ActivateTriggerIntents.Add(
                new ActivateTriggerIntent
                {
                    TriggerId = triggerReference.Id,
                    Value = trigger.ToTaskResult()
                });
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
                    Service = transitionContext.ServiceRef.Id,
                    Method = transitionContext.MethodId,
                    TaskId = ((IProxyTaskState)routineCompletionTask.AsyncState).TaskId
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
                var intent = (ExecuteRoutineIntent)userData;

                throw new NotImplementedException(
                    $"Need to await for the result in process for '{intent.Service.Name}.{intent.Method.Name}' " +
                    $"when called by {continuationInfo.Type} '{continuationObject.GetType()}'.");
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

            // NOTE:
            // In RELEASE mode a state machine is compiled into a value type, not a reference type.
            // The builder is also is a value type, so the only way to tell exactly is to compare
            // the completion Tasks, which are always reference types.
            if (asm1.GetType().IsValueType())
            {
#warning Optimize this code
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
