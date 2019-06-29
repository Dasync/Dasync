using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Triggers;
using Dasync.ExecutionEngine.Transitions;

namespace Dasync.ExecutionEngine.Triggers
{
    public class TaskCompletionSourceRegistry : ITaskCompletionSourceRegistry
    {
        private readonly IUniqueIdGenerator _numericIdGenerator;
        private readonly ITransitionScope _transitionScope;

        public TaskCompletionSourceRegistry(
            IUniqueIdGenerator numericIdGenerator,
            ITransitionScope transitionScope)
        {
            _numericIdGenerator = numericIdGenerator;
            _transitionScope = transitionScope;
        }

        public bool TryRegisterNew(object taskCompletionSource, out TriggerReference triggerReference)
        {
            if (taskCompletionSource == null)
                throw new ArgumentNullException(nameof(taskCompletionSource));
            if (!TaskCompletionSourceAccessor.IsTaskCompletionSource(taskCompletionSource))
                throw new ArgumentException($"Input object must be a TaskCompletionSource`1, but got {taskCompletionSource.GetType()}", nameof(taskCompletionSource));

            var task = TaskCompletionSourceAccessor.GetTask(taskCompletionSource);

            triggerReference = task.AsyncState as TriggerReference;
            if (triggerReference != null)
                return false;

            triggerReference = new TriggerReference
            {
                Id = _numericIdGenerator.NewId()
            };
            task.SetAsyncState(triggerReference);

            return true;
        }

        public bool Monitor(object taskCompletionSource)
        {
            if (taskCompletionSource == null)
                throw new ArgumentNullException(nameof(taskCompletionSource));
            if (!TaskCompletionSourceAccessor.IsTaskCompletionSource(taskCompletionSource))
                throw new ArgumentException($"Input object must be a TaskCompletionSource`1, but got {taskCompletionSource.GetType()}", nameof(taskCompletionSource));

            var task = TaskCompletionSourceAccessor.GetTask(taskCompletionSource);

            if (!(task.AsyncState is TriggerReference triggerReference))
                return false;

#warning Check if already subscribed to the completion.
            task.ContinueWith(OnTaskComplete, TaskContinuationOptions.ExecuteSynchronously);

            return true;
        }

        private void OnTaskComplete(Task task)
        {
            var triggerReference = (TriggerReference)task.AsyncState;
            if (_transitionScope.IsActive)
                _transitionScope.CurrentMonitor.ActivateTrigger(task, triggerReference);
        }
    }
}
