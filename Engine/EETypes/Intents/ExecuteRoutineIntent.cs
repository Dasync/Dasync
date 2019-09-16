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
        public string Id;

        public ServiceId ServiceId;

        public RoutineMethodId MethodId;

#warning Allow multiple continuations. Multicast continuation?
        public ContinuationDescriptor Continuation;

        public DateTimeOffset Timestamp = DateTimeOffset.Now;

        public IValueContainer Parameters;
    }
}
