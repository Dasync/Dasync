using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Serialization
{
    public static class TypeExtensions
    {
        public static TypeSerializationInfo ToTypeSerializationInfo(this Type type)
        {
            if (type == typeof(TypeSerializationInfo))
                return TypeSerializationInfo.Self;
#warning pre-cache
            return CreateTypeSerializationInfo(type);
        }

        internal static TypeSerializationInfo CreateTypeSerializationInfo(Type type)
        {
            return new TypeSerializationInfo
            {
                Name = type.GetFullName(),
                Assembly = type.GetAssembly().ToAssemblySerializationInfo(),
                GenericArgs = type.IsGenericType()
                    ? type.GetGenericArguments().Select(t => t.ToTypeSerializationInfo()).ToArray()
                    : null
            };
        }

        public static string GetFullName(this Type type)
        {
            return type.IsGenericType() && !type.IsGenericTypeDefinition()
                ? type.GetGenericTypeDefinition().FullName
                : type.FullName;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsEnum(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Type GetEnumUnderlyingType(this Type type)
        {
#if NETSTANDARD
            return Enum.GetUnderlyingType(type);
#else
            return type.GetEnumUnderlyingType();
#endif
        }

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
        internal static bool IsGenericTypeDefinition(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().IsGenericTypeDefinition;
#else
            return type.IsGenericTypeDefinition;
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
        internal static Type[] GetGenericArguments(this Type type)
        {
#if NETSTANDARD
            return type.GetTypeInfo().GenericTypeArguments;
#else
            return type.GetGenericArguments();
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
