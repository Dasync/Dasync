using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Accessors
{
    internal static class ReflectionExtensions
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetCustomAttribute<T>(this Type type) where T : Attribute
        {
#if NETSTANDARD
            return type.GetTypeInfo().GetCustomAttribute<T>();
#else
            return type.GetCustomAttribute<T>();
#endif
        }

#if NETFX
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, methodInfo);
        }
#endif
    }
}
