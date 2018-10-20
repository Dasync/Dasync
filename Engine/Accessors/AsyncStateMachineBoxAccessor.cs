using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dasync.Accessors
{
    public static class AsyncStateMachineBoxAccessor
    {
        private static readonly Type AsyncStateMachineBoxType =
            typeof(AsyncStateMachineAttribute).GetAssembly().GetType(
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1")
            .GetNestedType("AsyncStateMachineBox`1", BindingFlags.NonPublic);

        private static readonly Type IAsyncStateMachineBoxType =
            typeof(AsyncStateMachineAttribute).GetAssembly().GetType(
                "System.Runtime.CompilerServices.IAsyncStateMachineBox");

        // Cannot be pre-defined, because it's generic.
        //private static readonly FieldInfo _Context =
        //    AsyncStateMachineBoxType?.GetField("Context",
        //        BindingFlags.Instance | BindingFlags.Public);

        private static readonly MethodInfo _GetStateMachineObject =
            IAsyncStateMachineBoxType?.GetMethod("GetStateMachineObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool IsAsyncStateMachineBox(Type type) =>
            AsyncStateMachineBoxType != null &&
            type.IsGenericType() &&
            AsyncStateMachineBoxType.IsAssignableFrom(type.GetGenericTypeDefinition());

        public static ExecutionContext GetContext(object box)
        {
            var fiContext = box.GetType().GetField("Context", BindingFlags.Instance | BindingFlags.Public);
            return (ExecutionContext)fiContext.GetValue(box);
        }

        public static IAsyncStateMachine GetStateMachine(object box)
            => (IAsyncStateMachine)_GetStateMachineObject.Invoke(box, null);
    }
}
