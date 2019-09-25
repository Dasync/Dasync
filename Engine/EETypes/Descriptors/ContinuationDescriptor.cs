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

#warning Add state of the actual routine being resumed? That option would remove the need of persistant storage for the state - eveything is conveyed in messages. However, that can blow the size of a message - need overflow mechanism.
    }
}
