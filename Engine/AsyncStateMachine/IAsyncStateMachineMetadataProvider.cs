using System;

namespace Dasync.AsyncStateMachine
{
    public interface IAsyncStateMachineMetadataProvider
    {
        AsyncStateMachineMetadata GetMetadata(Type asyncStateMachineType);
    }
}
