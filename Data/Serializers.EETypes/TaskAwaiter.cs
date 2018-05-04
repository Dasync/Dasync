using System;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes
{
    public sealed class TaskAwaiterSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer Decompose(object value)
        {
            return new AwaiterContainer
            {
                Task = TaskAwaiterUtils.GetTask(value)
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var values = (AwaiterContainer)container;
            var awaiter = Activator.CreateInstance(valueType);
            TaskAwaiterUtils.SetTask(awaiter, values.Task);
            return awaiter;
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new AwaiterContainer();
        }
    }

    public sealed class AwaiterContainer : ValueContainerBase
    {
        public Task Task;
    }
}
