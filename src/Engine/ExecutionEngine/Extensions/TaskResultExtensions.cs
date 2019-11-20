using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes.Descriptors;

namespace Dasync.ExecutionEngine.Extensions
{
    public static class TaskResultExtensions
    {
        public static ITaskResult ToTaskResult(this Task task)
        {
            var status = task.Status;
            if (status != TaskStatus.RanToCompletion && status != TaskStatus.Canceled && status != TaskStatus.Faulted)
                throw new ArgumentException($"The task is not completed and is in '{status}' state.", nameof(task));

            var exception = (task.Exception is AggregateException aggregateException && aggregateException.InnerExceptions?.Count == 1)
                ? aggregateException.InnerException
                : task.Exception;

            var valueType = task.GetResultType();
            if (valueType == null ||
                valueType == typeof(void) ||
                valueType == TaskAccessor.VoidTaskResultType ||
                valueType == typeof(object))
            {
                return new TaskResult
                {
                    Value = status == TaskStatus.RanToCompletion ? task.GetResult() : null,
                    Exception = status == TaskStatus.Faulted ? exception : null,
                    IsCanceled = status == TaskStatus.Canceled
                };
            }
            else
            {
                return TaskResult.Create(
                    valueType,
                    status == TaskStatus.RanToCompletion ? task.GetResult() : null,
                    status == TaskStatus.Faulted ? exception : null,
                    status == TaskStatus.Canceled);
            }
        }

        public static bool TrySetResult(this Task task, ITaskResult result)
        {
            if (result.IsCanceled)
                return task.TrySetCanceled();

            if (result.IsFaulted())
                return task.TrySetException(result.Exception);

            return task.TrySetResult(result.Value);
        }
    }
}
