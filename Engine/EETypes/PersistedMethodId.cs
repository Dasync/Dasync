namespace Dasync.EETypes
{
    public class PersistedMethodId : MethodId
    {
        /// <summary>
        /// An ID of the intent that triggered execution of this routine.
        /// Used for correlation purposes.
        /// </summary>
        public string IntentId { get; set; }

        public string RoutineId { get; set; }

        public string ETag { get; set; }
    }
}
