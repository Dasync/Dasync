using System.Collections.Generic;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Communication
{
    public class MethodInvocationData
    {
        public string IntentId { get; set; }

        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public IValueContainer Parameters { get; set; }
    }
}
