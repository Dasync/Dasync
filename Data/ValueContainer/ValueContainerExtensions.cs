using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.ValueContainer
{
    public static class ValueContainerExtensions
    {
        public static IEnumerable<NamedValue> ToEnumerable(this IValueContainer container)
            => container as IEnumerable<NamedValue> ?? new ValueContainerEnumerable(container);

        public static ValueContainer Add<T>(this ValueContainer container, string name, T value = default(T))
        {
            container.Add(
                new NamedValue
                {
                    Index = container.Count,
                    Name = name,
                    Type = typeof(T),
                    Value = value
                });
            return container;
        }

        public static ValueContainer Add(this ValueContainer container, string name, Type type, object value = null)
        {
            container.Add(
                new NamedValue
                {
                    Index = container.Count,
                    Name = name,
                    Type = type,
                    Value = value
                });
            return container;
        }

        public static IValueContainer Clone(this IValueContainer container)
        {
            var copy = new ValueContainer();
            var valueCount = container.GetCount();
            for (var i = 0; i < valueCount; i++)
                copy.Add(container.GetName(i), container.GetType(i), container.GetValue(i));
            return copy;
        }

        public static void CopyTo(this IValueContainer source, IValueContainer target)
        {
            if (source == null || target == null || source.GetCount() == 0 || target.GetCount() == 0)
                return;

            var sourceValues = source.ToEnumerable().ToDictionary(v => v.Name);
            int targetCount = target.GetCount();
            for (var i = 0; i < targetCount; i++)
            {
                var name = target.GetName(i);
                if (!sourceValues.TryGetValue(name, out var v) || v.Type != target.GetType(i))
                    continue;
                target.SetValue(i, v.Value);
            }
        }
    }
}
