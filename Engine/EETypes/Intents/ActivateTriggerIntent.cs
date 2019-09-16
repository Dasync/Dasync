using System.Runtime.InteropServices;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class ActivateTriggerIntent
    {
        public string TriggerId;

        public TaskResult Value;
    }
}
