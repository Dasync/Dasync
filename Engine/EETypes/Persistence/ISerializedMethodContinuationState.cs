namespace Dasync.EETypes.Persistence
{
    /// <summary>
    /// Exexution state of a paused routine.
    /// </summary>
    public interface ISerializedMethodContinuationState
    {
        /// <summary>
        /// The Format of an ISerializer
        /// </summary>
        string Format { get; set; }

        /// <summary>
        /// Serialized method execution state
        /// </summary>
        byte[] State { get; set; }
    }
}
