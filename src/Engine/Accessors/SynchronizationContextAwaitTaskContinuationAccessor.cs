using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class SynchronizationContextAwaitTaskContinuationAccessor
    {
        public static readonly Type SynchronizationContextAwaitTaskContinuationType =
            typeof(Task).GetAssembly().GetType(
                "System.Threading.Tasks.SynchronizationContextAwaitTaskContinuation",
                throwOnError: true, ignoreCase: false);

        private static readonly FieldInfo _actionField =
            SynchronizationContextAwaitTaskContinuationType
            .GetField("action", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Type WrapperDelegateType;
        private static readonly FieldInfo ActionFieldInfo;

        static SynchronizationContextAwaitTaskContinuationAccessor()
        {
            var nestedTypes = SynchronizationContextAwaitTaskContinuationType.GetNestedTypes(
                BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var nt in nestedTypes)
            {
                ActionFieldInfo = nt.GetField("action", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (ActionFieldInfo != null)
                {
                    WrapperDelegateType = nt;
                    break;
                }
            }
        }

        public static bool TryGetAction(object continuation, out Action action)
        {
            if (continuation == null)
                throw new ArgumentNullException(nameof(continuation));

            if (ReferenceEquals(continuation.GetType(), SynchronizationContextAwaitTaskContinuationType))
            {
                action = (Action)_actionField.GetValue(continuation);
                return true;
            }
            else if (ReferenceEquals(WrapperDelegateType, continuation.GetType()))
            {
                action = (Action)ActionFieldInfo.GetValue(continuation);
                return true;
            }
            else
            {
                action = null;
                return false;
            }
        }
    }
}
