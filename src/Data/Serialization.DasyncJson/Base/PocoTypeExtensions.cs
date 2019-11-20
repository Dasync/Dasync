using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Serialization
{
    public static class PocoTypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, bool> _pocoTypeMap
            = new ConcurrentDictionary<Type, bool>(new[]
            {
                new KeyValuePair<Type, bool>(typeof(object), true),
                new KeyValuePair<Type, bool>(typeof(ValueType), true)
            });

        private static readonly Func<Type, bool> _checkIsPoco = CheckIsPoco;

        private static bool CheckIsPoco(Type type)
        {
            if (!IsPoco(type.GetTypeInfo().BaseType))
                return false;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            if (type.GetEvents(flags).Length != 0)
                return false;

            foreach (var fi in type.GetFields(flags))
            {
                if (fi.IsInitOnly)
                    return false;

                if (fi.IsPrivate)
                {
                    if (fi.Name[0] == '<' && fi.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                    {
                        var propertyName = fi.Name.Substring(1, fi.Name.IndexOf('>') - 1);
                        var property = type.GetProperty(propertyName, flags);
                        if (property != null && property.GetMethod != null && property.SetMethod != null
                            && property.GetMethod.IsPublic)
                        {
                            continue;
                        }
                    }
                    return false;
                }

                continue;
            }

            // Must have 1 default constructor that does not take in any arguments.
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var hasDefaultCtor = ctors.Length == 0 || (ctors.Length >= 1 && ctors.Any(ctor => ctor.GetParameters().Length == 0));
            if (!hasDefaultCtor)
                return false;

            return true;
        }

        public static bool IsPoco(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.IsGenericType())
                type = type.GetGenericTypeDefinition();

            return _pocoTypeMap.GetOrAdd(type, _checkIsPoco);
        }
    }
}
