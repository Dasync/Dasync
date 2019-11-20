using System.Reflection;

namespace Dasync.AsyncStateMachine
{
    public static class AsyncStateMachineMetadataProviderExtensions
    {
        public static AsyncStateMachineMetadata GetMetadata(this IAsyncStateMachineMetadataProvider provider, MethodInfo methodInfo)
            => provider.GetMetadata(MethodInfoToStateMachineTypeConverter.GetStateMachineType(methodInfo));
    }
}
