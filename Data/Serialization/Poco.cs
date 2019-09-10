using System;
using System.Reflection;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public sealed class PocoSerializer : IObjectDecomposer, IObjectComposer
    {
        public static readonly PocoSerializer Instance = new PocoSerializer();

        public IValueContainer Decompose(object value)
        {
            return ValueContainerFactory.CreateProxy(value);
        }

        public IValueContainer CreatePropertySet(Type type)
        {
#warning Optimize it
            var instance = Activator.CreateInstance(type);
            return ValueContainerFactory.CreateProxy(instance);
        }

        public object Compose(IValueContainer container, Type valueType)
        {
#warning Add IValueContainerAsAccessor interface (or better name) which exposes the @source
            var sourceField = container.GetType().GetField("@source", BindingFlags.Instance | BindingFlags.NonPublic);
            return sourceField.GetValue(container);
        }
    }
}
