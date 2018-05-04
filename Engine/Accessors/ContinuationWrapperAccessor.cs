using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class ContinuationWrapperAccessor
    {
#warning TODO: pre-compile GetContinuation, GetInvokeAction, and GetInnerTask

        public static readonly Type ContinuationWrapperType =
            typeof(AsyncStateMachineAttribute).GetAssembly().GetType(
                "System.Runtime.CompilerServices.AsyncMethodBuilderCore")
            .GetNestedType("ContinuationWrapper", BindingFlags.NonPublic);

        private static readonly FieldInfo _fi_m_continuation =
            ContinuationWrapperType.GetField("m_continuation",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo _fi_m_invokeAction =
            ContinuationWrapperType.GetField("m_invokeAction",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo _fi_m_innerTask =
            ContinuationWrapperType.GetField("m_innerTask",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static Action GetContinuation(object continuationWrapper)
            => (Action)_fi_m_continuation.GetValue(continuationWrapper);

        public static Action GetInvokeAction(object continuationWrapper)
            => (Action)_fi_m_invokeAction.GetValue(continuationWrapper);

        public static Task GetInnerTask(object continuationWrapper)
            => (Task)_fi_m_innerTask.GetValue(continuationWrapper);
    }
}
