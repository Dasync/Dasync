using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;

namespace Dasync.Persistence.FileSystem
{
    public class MethodExecutionStateDto
    {
        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public string ContentType { get; set; }

        public byte[] StateData { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public CallerDescriptor Caller { get; set; }

        public string CallerContentType { get; set; }

        public byte[] CallerStateData { get; set; }
    }
}
