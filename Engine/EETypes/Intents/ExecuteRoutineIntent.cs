using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Intents
{
    public class ExecuteRoutineIntent
    {
        /// <summary>
        /// An intent ID, which must be unique within a scope of a routine being executed.
        /// </summary>
        public long Id;

        public ServiceId ServiceId;

        public RoutineMethodId MethodId;

        public IValueContainer Parameters;

#warning Allow multiple continuations. Multicast continuation?
        public ContinuationDescriptor Continuation;

        /// <summary>
        /// The calling service+routine, if any.
        /// NULL when is called outside of the transitioning context.
        /// </summary>
        public CallerDescriptor Caller;

        /// <summary>
        /// A hint to notify current process on routine completion (synchronous call).
        /// </summary>
        public bool NotifyOnCompletion;
    }
}
