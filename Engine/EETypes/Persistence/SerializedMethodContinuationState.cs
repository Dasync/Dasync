namespace Dasync.EETypes.Persistence
{
    /// <summary>
    /// Exexution state of a paused routine.
    /// </summary>
    public class SerializedMethodContinuationState
    {
        /// <summary>
        /// The Format of an ISerializer
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Serialized method execution state
        /// </summary>
        public byte[] State { get; set; }
    }
}
