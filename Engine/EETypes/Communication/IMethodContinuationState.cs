namespace Dasync.EETypes.Communication
{
    /// <summary>
    /// Exexution state of a paused routine.
    /// </summary>
    public interface IMethodContinuationState
    {
        string ContentType { get; set; }

        byte[] State { get; set; }
    }
}
