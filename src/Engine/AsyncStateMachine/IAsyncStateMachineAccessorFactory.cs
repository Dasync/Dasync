namespace Dasync.AsyncStateMachine
{
    public interface IAsyncStateMachineAccessorFactory
    {
        IAsyncStateMachineAccessor Create(AsyncStateMachineMetadata metadata);
    }
}
