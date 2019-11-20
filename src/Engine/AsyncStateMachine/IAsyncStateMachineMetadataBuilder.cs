using System;

namespace Dasync.AsyncStateMachine
{
    public interface IAsyncStateMachineMetadataBuilder
    {
        AsyncStateMachineMetadata Build(Type stateMachineType);
    }
}
