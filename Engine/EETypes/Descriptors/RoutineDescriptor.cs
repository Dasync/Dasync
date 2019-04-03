namespace Dasync.EETypes.Descriptors
{
    public sealed class RoutineDescriptor
    {
        /// <summary>
        /// An ID of the intent that triggered execution of this routine.
        /// Used for correlation purposes.
        /// </summary>
        public string IntentId;

        public RoutineMethodId MethodId;

        public string RoutineId;

        public string ETag;
    }
}
