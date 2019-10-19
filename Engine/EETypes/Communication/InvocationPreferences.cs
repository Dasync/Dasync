namespace Dasync.EETypes.Communication
{
    public struct InvocationPreferences
    {
        /// <summary>
        /// Lock message immediately at the publish time so it can be processed right away.
        /// </summary>
        public bool LockMessage { get; set; }

        /// <summary>
        /// Prefer to receive the result right away instead of saving the state and waiting for continuation.
        /// </summary>
        public bool Synchronous { get; set; }
    }
}
