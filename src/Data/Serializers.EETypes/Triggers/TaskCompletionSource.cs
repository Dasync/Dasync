using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes.Triggers;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes.Triggers
{
    public class TaskCompletionSourceSerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly ITaskCompletionSourceRegistry _taskCompletionSourceRegistry;

        public TaskCompletionSourceSerializer(ITaskCompletionSourceRegistry taskCompletionSourceRegistry)
        {
            _taskCompletionSourceRegistry = taskCompletionSourceRegistry;
        }

        public IValueContainer Decompose(object value)
        {
            return new TaskCompletionSourceContainer
            {
                Task = TaskCompletionSourceAccessor.GetTask(value)
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var values = (TaskCompletionSourceContainer)container;
            if (values.Task == null) // must not be NULL!
                return TaskCompletionSourceAccessor.Create(typeof(object));

            var taskCompletionSource = TaskCompletionSourceAccessor.Create(values.Task);
            _taskCompletionSourceRegistry.Monitor(taskCompletionSource);
            return taskCompletionSource;
        }

        public IValueContainer CreatePropertySet(Type valueType)
            => new TaskCompletionSourceContainer();
    }

    public class TaskCompletionSourceContainer : ValueContainerBase
    {
        public Task Task;
    }
}
