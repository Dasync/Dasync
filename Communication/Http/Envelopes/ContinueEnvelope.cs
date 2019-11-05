using System;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.Communication.Http.Envelopes
{
    public class ContinueEnvelope
    {
        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public string TaskId { get; set; }

        public DateTimeOffset? ContinueAt { get; set; }

        public CallerDescriptor Caller { get; set; }

        public IValueContainer Result { get; set; }

        public string ContinuationStateFormat { get; set; }

        public byte[] ContinuationStateData { get; set; }
    }
}
