using System;
using System.Collections.Generic;
using System.Reflection;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Collections
{
    public sealed class ListSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer CreatePropertySet(Type valueType)
        {
            return (IValueContainer)Activator.CreateInstance(
                typeof(ListContainer<>)
                .MakeGenericType(valueType.GetGenericArguments()[0]));
        }

        public IValueContainer Decompose(object value)
        {
            return (IValueContainer)
                typeof(ListSerializer)
                .GetMethod(nameof(Decompose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(value.GetType().GetGenericArguments()[0])
                .Invoke(null, new object[] { value });
        }

        private static IValueContainer Decompose<T>(List<T> value)
        {
            return new ListContainer<T>
            {
                Items = value.ToArray()
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            return typeof(ListSerializer)
                .GetMethod(nameof(Compose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(valueType.GetGenericArguments()[0])
                .Invoke(null, new object[] { container, valueType });
        }

        private static object Compose<T>(IValueContainer container, Type valueType)
        {
            return new List<T>(((ListContainer<T>)container).Items);
        }
    }

    public sealed class ListContainer<T> : ValueContainerBase
    {
        public T[] Items;
    }
}
