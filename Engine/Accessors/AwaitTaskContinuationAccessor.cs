using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class AwaitTaskContinuationAccessor
    {
        public static readonly Type AwaitTaskContinuationType =
            typeof(Task).GetAssembly().GetType(
                "System.Threading.Tasks.AwaitTaskContinuation", throwOnError: true, ignoreCase: false);

        private static readonly FieldInfo _actionField = AwaitTaskContinuationType.GetField(
            "m_action", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo _contextField = AwaitTaskContinuationType.GetField(
            "m_capturedContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool IsAwaitTaskContinuation(object continuation)
            => AwaitTaskContinuationType.IsAssignableFrom(continuation.GetType());

        public static Action GetAction(object continuation)
            => (Action)_actionField.GetValue(continuation);

        public static ExecutionContext GetContext(object continuation)
            => (ExecutionContext)_contextField.GetValue(continuation);
    }
}
