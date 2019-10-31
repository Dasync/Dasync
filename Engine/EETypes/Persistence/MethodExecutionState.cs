using System.Collections.Generic;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Persistence
{
    public class MethodExecutionState
    {
        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public IValueContainer MethodState { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public SerializedMethodContinuationState ContinuationState { get; set; }

        public CallerDescriptor Caller { get; set; }
    }
}
