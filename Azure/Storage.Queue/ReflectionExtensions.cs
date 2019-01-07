using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Azure.Storage.Queue
{
    internal static class ReflectionExtensions
    {
#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PropertyInfo GetProperty(this Type type, string propertyName)
        {
            return type.GetTypeInfo().GetDeclaredProperty(propertyName);
        }
#endif
    }
}
