using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dasync.Fabric.InMemory
{
    internal static class ReflectionExtensions
    {
#if !NETFX && !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ExecutionContext CreateCopy(this ExecutionContext executionContext)
        {
            var miCreateCopy = executionContext.GetType().GetTypeInfo().GetDeclaredMethod("CreateCopy");
            if (miCreateCopy != null)
                return (ExecutionContext)miCreateCopy.Invoke(executionContext, parameters: null);
            return executionContext;
        }
#endif
    }
}
