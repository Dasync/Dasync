using System;
using System.Runtime.InteropServices;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class RegisterTriggerIntent
    {
        public string TriggerId { get; set; }

        public Type ValueType { get; set; }
    }
}
