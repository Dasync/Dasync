namespace Dasync.EETypes.Communication
{
    public struct ContinueRoutineResult
    {
        /// <summary>
        /// Optional message handle. Must be available if the <see cref="InvocationPreferences.LockMessage"/>
        /// flag is set and the communicator supports the feature.
        /// </summary>
        public IMessageHandle MessageHandle { get; set; }
    }
}
