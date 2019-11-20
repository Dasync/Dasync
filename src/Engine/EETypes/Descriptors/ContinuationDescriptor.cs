using System;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Descriptors
{
    public class ContinuationDescriptor
    {
        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public DateTime? ContinueAt { get; set; }

        /// <summary>
        /// The <see cref="ExecuteRoutineIntent.Id"/> for awaited routine, which will be
        /// used to correlate serialized proxy tasks with <see cref="ContinueRoutineIntent.Result"/>.
        /// </summary>
        public string TaskId { get; set; }
    }
}
