using System.Runtime.InteropServices;
using Dasync.CloudEvents;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Transitions;
using Newtonsoft.Json;

namespace Dasync.Fabric.FileBased
{
    [StructLayout(LayoutKind.Sequential)]
    public class RoutineDataEnvelope
    {
        public RoutineStatus Status { get; set; }

        public ServiceId ServiceId { get; set; }

        public CallerDescriptor Caller { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        [JsonConverter(typeof(NestedJsonConverter))]
        public string State { get; set; }

        [JsonConverter(typeof(NestedJsonConverter))]
        public string Result { get; set; }
    }
}
