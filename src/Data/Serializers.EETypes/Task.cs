using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes
{
    public sealed class TaskSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer Decompose(object value)
        {
            var task = (Task)value;

            object result = null;
            if (task.Status == TaskStatus.RanToCompletion &&
                !task.IsVoidResult())
                result = task.GetResult();

            var exception = task.IsFaulted ? task.Exception : null;

            TaskCapture.CaptureTask(task);

            return new TaskContainer
            {
                State = task.AsyncState,
                Status = task.Status,
                Result = result,
                Exception = exception,
                // TODO: Options = task.CreationOptions
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var values = (TaskContainer)container;

            var resultType = TaskAccessor.GetTaskResultType(valueType);

            Task task = TaskAccessor.CreateTask(values.State, resultType);
            if (values.Status == TaskStatus.Canceled)
            {
                task.TrySetCanceled();
            }
            else if (values.Status == TaskStatus.Faulted)
            {
                task.TrySetException(values.Exception);
            }
            else if (values.Status == TaskStatus.RanToCompletion)
            {
                var result = values.Result;
                //if (result != null && resultType != result.GetType())
                //    result = Convert.ChangeType(result, resultType);
                task.TrySetResult(result);
            }

            task.SetStatus(values.Status);

            // TODO: task.SetCreationOptions(values.Options);

            return task;
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new TaskContainer();
        }
    }

    public sealed class TaskContainer : ValueContainerBase
    {
        public TaskStatus Status;

        public object Result;

        public Exception Exception;

        public object State;

        // TODO: Task.m_stateFlags & 65535
        //public TaskCreationOptions Options;
    }
}
