namespace Dasync.EETypes.Persistence
{
    /// <summary>
    /// Exexution state of a paused routine.
    /// </summary>
    public interface ISerializedMethodContinuationState
    {
        string ContentType { get; set; }

        byte[] State { get; set; }
    }
}
