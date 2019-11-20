using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.AsyncStateMachine
{
    public static class Extensions_MethodInfo
    {
        public static bool IsAsyncStateMachine(this MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null;
        }
    }
}
