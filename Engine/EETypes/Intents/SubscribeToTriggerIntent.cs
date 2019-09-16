using System.Runtime.InteropServices;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class SubscribeToTriggerIntent
    {
        public string TriggerId;

        public ContinuationDescriptor Continuation;
    }
}
