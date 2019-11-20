using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.ExecutionEngine
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsInterface(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().IsInterface;
#else
            return type.IsInterface;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValueType(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IEnumerable<Type> GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces;
        }
#endif

#if NETFX
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static MethodInfo GetMethodInfo(this Delegate @delegate)
        {
            return @delegate.Method;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Intern(this string str)
        {
#warning Supported in .NET Standard 2.0
#if NETSTANDARD
            return str;
#else
            return string.Intern(str);
#endif
        }
    }
}
