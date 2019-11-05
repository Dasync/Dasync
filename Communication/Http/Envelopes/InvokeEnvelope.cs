using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.Communication.Http.Envelopes
{
    public class InvokeEnvelope
    {
        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public IValueContainer Parameters { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public string ContinuationStateFormat { get; set; }

        public byte[] ContinuationStateData { get; set; }
    }
}
