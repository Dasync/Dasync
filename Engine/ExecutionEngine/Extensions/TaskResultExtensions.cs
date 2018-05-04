using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes.Descriptors;

namespace Dasync.ExecutionEngine.Extensions
{
    public static class TaskResultExtensions
    {
        public static TaskResult ToTaskResult(this Task task)
        {
            var status = task.Status;
            if (status != TaskStatus.RanToCompletion && status != TaskStatus.Canceled && status != TaskStatus.Faulted)
                throw new ArgumentException($"The task is not completed and is in '{status}' state.", nameof(task));

            return new TaskResult
            {
                Value = status == TaskStatus.RanToCompletion ? task.GetResult() : null,
                Exception = status == TaskStatus.Faulted ? task.Exception : null,
                IsCanceled = status == TaskStatus.Canceled
            };
        }

        public static bool TrySetResult(this Task task, TaskResult result)
        {
            if (result.IsCanceled)
                return task.TrySetCanceled();

            if (result.IsFaulted)
                return task.TrySetException(result.Exception);

            return task.TrySetResult(result.Value);
        }
    }
}
