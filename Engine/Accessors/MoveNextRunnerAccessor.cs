using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dasync.Accessors
{
    public static class MoveNextRunnerAccessor
    {
#warning TODO: pre-compile GetContext and GetStateMachine

        // MoveNextRunner has been replaced with AsyncStateMachineBox starting from .NET Core 2.1
        private static readonly Type MoveNextRunnerType =
            typeof(AsyncStateMachineAttribute).GetAssembly().GetType(
                "System.Runtime.CompilerServices.AsyncMethodBuilderCore")
            .GetNestedType("MoveNextRunner", BindingFlags.NonPublic);

        private static readonly FieldInfo _fi_m_context =
            MoveNextRunnerType?.GetField("m_context",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo _fi_m_stateMachine =
            MoveNextRunnerType?.GetField("m_stateMachine",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsMoveNextRunner(Type type) => MoveNextRunnerType != null && MoveNextRunnerType.IsAssignableFrom(type);

        public static ExecutionContext GetContext(object moveNextRunner)
            => (ExecutionContext)_fi_m_context?.GetValue(moveNextRunner);

        public static IAsyncStateMachine GetStateMachine(object moveNextRunner)
            => (IAsyncStateMachine)_fi_m_stateMachine.GetValue(moveNextRunner);
    }
}
