namespace Dasync.EETypes.Triggers
{
    public interface ITaskCompletionSourceRegistry
    {
        bool TryRegisterNew(object taskCompletionSource, out TriggerReference triggerReference);

        bool Monitor(object taskCompletionSource);
    }
}
