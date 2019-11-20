using System.Runtime.InteropServices;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class SubscribeToTriggerIntent
    {
        public string TriggerId { get; set; }

        public ContinuationDescriptor Continuation { get; set; }
    }
}
