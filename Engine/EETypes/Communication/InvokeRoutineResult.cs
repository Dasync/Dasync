using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Communication
{
    public struct InvokeRoutineResult
    {
        public InvocationOutcome Outcome { get; set; }

        /// <summary>
        /// Available only if the routine has completed synchronously. See <see cref="InvocationPreferences.Synchronous"/>.
        /// </summary>
        public TaskResult Result { get; set; }

        /// <summary>
        /// Optional message handle. Must be available if the <see cref="InvocationPreferences.LockMessage"/>
        /// flag is set and the communicator supports the feature.
        /// </summary>
        public IMessageHandle MessageHandle { get; set; }
    }
}
