using System;
using System.Runtime.InteropServices;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class RegisterTriggerIntent
    {
        public string TriggerId;

        public Type ValueType;
    }
}
