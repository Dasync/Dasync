using System.Reflection;

namespace Dasync.AsyncStateMachine
{
    public static class AsyncStateMachineMetadataBuilderExtensions
    {
        public static AsyncStateMachineMetadata Build(this IAsyncStateMachineMetadataBuilder builder, MethodInfo methodInfo)
            => builder.Build(MethodInfoToStateMachineTypeConverter.GetStateMachineType(methodInfo));
    }
}
