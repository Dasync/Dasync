using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Serializers.EETypes
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

#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAssignableFrom(this Type type, Type otherType)
        {
            return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
        }
#endif
    }
}
