using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Collections
{
    public sealed class DictionarySerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer CreatePropertySet(Type valueType)
        {
            return (IValueContainer)Activator.CreateInstance(
                typeof(DictionaryContainer<,>)
                .MakeGenericType(valueType.GetGenericArguments()[0], valueType.GetGenericArguments()[1]));
        }

        public IValueContainer Decompose(object value)
        {
            return (IValueContainer)
                typeof(DictionarySerializer)
                .GetMethod(nameof(Decompose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(value.GetType().GetGenericArguments()[0], value.GetType().GetGenericArguments()[1])
                .Invoke(null, new object[] { value });
        }

        private static IValueContainer Decompose<TKey, TValue>(Dictionary<TKey, TValue> value)
        {
            return new DictionaryContainer<TKey, TValue>
            {
                Items = value.ToArray()
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            return typeof(DictionarySerializer)
                .GetMethod(nameof(Compose), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(valueType.GetGenericArguments()[0], valueType.GetGenericArguments()[1])
                .Invoke(null, new object[] { container, valueType });
        }

        private static object Compose<TKey, TValue>(IValueContainer container, Type valueType)
        {
            var propertySet = (DictionaryContainer<TKey, TValue>)container;
            return propertySet.Items.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }

    public sealed class DictionaryContainer<TKey, TValue> : ValueContainerBase
    {
        public KeyValuePair<TKey, TValue>[] Items;
    }
}
