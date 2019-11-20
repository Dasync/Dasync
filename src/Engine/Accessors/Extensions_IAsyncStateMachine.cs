using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.Accessors
{
    public static class Extensions_IAsyncStateMachine
    {
        public static bool IsCompilerGenerated(this IAsyncStateMachine asyncStateMachine)
        {
            return asyncStateMachine.GetType().GetCustomAttribute<CompilerGeneratedAttribute>() != null;
        }
    }
}
