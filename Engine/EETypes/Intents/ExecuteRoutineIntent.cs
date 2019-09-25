using System;
using System.Runtime.InteropServices;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class ExecuteRoutineIntent
    {
        /// <summary>
        /// An intent ID, which must be unique within a scope of a routine being executed.
        /// </summary>
        public string Id { get; set; }

        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

#warning Allow multiple continuations. Multicast continuation?
        public ContinuationDescriptor Continuation { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        public IValueContainer Parameters { get; set; }
    }
}
