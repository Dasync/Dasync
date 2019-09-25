using System.Runtime.InteropServices;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class SaveStateIntent
    {
        public ServiceId Service { get; set; }

#warning TODO: remove state of a service? Replace with a domain entity?
        //public IValueContainer ServiceState;

        public PersistedMethodId Method { get; set; }

        /// <summary>
        /// If not null, the save state is caused due to calling another
        /// routine which must resume the current one upon completion.
        /// </summary>
        public ExecuteRoutineIntent AwaitedRoutine { get; set; }

        public IValueContainer RoutineState { get; set; }

        public TaskResult RoutineResult { get; set; }
    }
}
