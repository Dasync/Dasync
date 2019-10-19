namespace Dasync.EETypes.Communication
{
    public enum InvocationOutcome
    {
        /// <summary>
        /// Method ran to its completion.
        /// </summary>
        Complete = 1,

        /// <summary>
        /// Method invocation has been scheduled to run asynchronously.
        /// </summary>
        Scheduled = 2,

        /// <summary>
        /// Routine has partially executed and is awaiting on a response.
        /// </summary>
        Paused = 3,

        /// <summary>
        /// Invocation has been dismissed as a duplicate message.
        /// </summary>
        Deduplicated = 4,
    }
}
