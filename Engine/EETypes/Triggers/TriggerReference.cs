namespace Dasync.EETypes.Triggers
{
    /// <summary>
    /// Used in <see cref="Task.AsyncState"/> to correlate to a trigger.
    /// </summary>
    public class TriggerReference : IProxyTaskState
    {
        public long Id;

        long IProxyTaskState.CorellationId => Id;
    }
}
