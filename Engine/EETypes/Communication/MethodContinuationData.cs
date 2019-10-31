using System;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Communication
{
    public class MethodContinuationData
    {
        /// <summary>
        /// Unique ID of this intent to continue executing a method.
        /// </summary>
        public string IntentId { get; set; }

        /// <summary>
        /// Unique ID of the awaited method or the <see cref="IProxyTaskState.TaskId"/> of a trigger.
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// The service which has a method to continue.
        /// </summary>
        public ServiceId Service { get; set; }

        /// <summary>
        /// The method to continue.
        /// </summary>
        public PersistedMethodId Method { get; set; }

        public DateTimeOffset? ContinueAt { get; set; }

        /// <summary>
        /// The awaited method or NULL if a trigger.
        /// </summary>
        public CallerDescriptor Caller { get; set; }

        /// <summary>
        /// The result of an awaited method or a trigger (represents <see cref="TaskResult"/>).
        /// </summary>
        public IValueContainer Result { get; set; }
    }
}
