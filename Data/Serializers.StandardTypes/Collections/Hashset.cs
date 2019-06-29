using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Collections
{
    public sealed class HashsetSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer CreatePropertySet(Type valueType)
        {
            return (IValueContainer)Activator.CreateInstance(
                typeof(HashsetContainer<>)
                .MakeGenericType(valueType.GetGenericArguments()[0]));
        }

        public IValueContainer Decompose(object value)
        {
            return (IValueContainer)
                typeof(HashsetSerializer)
                .GetMethod(nameof(Decompose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(value.GetType().GetGenericArguments()[0])
                .Invoke(null, new object[] { value });
        }

        private static IValueContainer Decompose<T>(HashSet<T> value)
        {
            return new HashsetContainer<T>
            {
                Items = value.ToArray()
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            return typeof(HashsetSerializer)
                .GetMethod(nameof(Compose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(valueType.GetGenericArguments()[0])
                .Invoke(null, new object[] { container, valueType });
        }

        private static object Compose<T>(IValueContainer container, Type valueType)
        {
            return new HashSet<T>(((HashsetContainer<T>)container).Items);
        }
    }

    public sealed class HashsetContainer<T> : ValueContainerBase
    {
        public T[] Items;
    }
}
