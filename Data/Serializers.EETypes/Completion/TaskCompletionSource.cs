using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes.Completion
{
    public class TaskCompletionSourceSerializer : IObjectDecomposer, IObjectComposer
    {
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

            return TaskCompletionSourceAccessor.Create(values.Task);
        }

        public IValueContainer CreatePropertySet(Type valueType)
            => new TaskCompletionSourceContainer();
    }

    public class TaskCompletionSourceContainer : ValueContainerBase
    {
        public Task Task;
    }
}
