using System;
using System.Collections.Generic;
using System.Reflection;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Collections
{
    public sealed class KeyValuePairSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer CreatePropertySet(Type valueType)
        {
            return (IValueContainer)Activator.CreateInstance(
                typeof(KeyValuePairContainer<,>)
                .MakeGenericType(valueType.GetGenericArguments()[0], valueType.GetGenericArguments()[1]));
        }

        public IValueContainer Decompose(object value)
        {
            return (IValueContainer)
                typeof(KeyValuePairSerializer)
                .GetMethod(nameof(Decompose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(value.GetType().GetGenericArguments()[0], value.GetType().GetGenericArguments()[1])
                .Invoke(null, new object[] { value });
        }

        private static IValueContainer Decompose<TKey, TValue>(KeyValuePair<TKey, TValue> value)
        {
            return new KeyValuePairContainer<TKey, TValue>
            {
                Key = value.Key,
                Value = value.Value
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            return typeof(KeyValuePairSerializer)
                .GetMethod(nameof(Compose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(valueType.GetGenericArguments()[0], valueType.GetGenericArguments()[1])
                .Invoke(null, new object[] { container, valueType });
        }

        private static object Compose<TKey, TValue>(IValueContainer container, Type valueType)
        {
            var propertySet = (KeyValuePairContainer<TKey, TValue>)container;
            return new KeyValuePair<TKey, TValue>(propertySet.Key, propertySet.Value);
        }
    }

    public sealed class KeyValuePairContainer<TKey, TValue> : ValueContainerBase
    {
        public TKey Key;
        public TValue Value;
    }
}
