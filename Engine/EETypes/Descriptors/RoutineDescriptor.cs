namespace Dasync.EETypes.Descriptors
{
    public sealed class RoutineDescriptor
    {
        /// <summary>
        /// An ID of the intent that triggered execution of this routine.
        /// Used for correlation purposes.
        /// </summary>
        public long IntentId;

        public RoutineMethodId MethodId;

        public string RoutineId;

        public string ETag;
    }
}
