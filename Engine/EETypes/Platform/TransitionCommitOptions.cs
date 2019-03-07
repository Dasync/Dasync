namespace Dasync.EETypes.Platform
{
    public struct TransitionCommitOptions
    {
        /// <summary>
        /// A hint to notify current process on routine completion (synchronous call).
        /// The listener must use <see cref="IRoutineCompletionNotifier.NotifyCompletion"/>.
        /// </summary>
        public bool NotifyOnRoutineCompletion;
    }
}
