using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Serializers.StandardTypes
{
    internal static class TypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Assembly GetAssembly(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().Assembly;
#else
            return type.Assembly;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Type GetBaseType(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().BaseType;
#else
            return type.BaseType;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsGenericType(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsGenericTypeDefinition(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().IsGenericTypeDefinition;
#else
            return type.IsGenericTypeDefinition;
#endif
        }

#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAssignableFrom(this Type type, Type otherType)
        {
            return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
        }
#endif
    }
}
