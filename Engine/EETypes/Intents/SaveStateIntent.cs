using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Intents
{
    public sealed class SaveStateIntent
    {
        public ServiceId ServiceId;

#warning TODO: remove state of a service? Replace with a domain entity?
        public IValueContainer ServiceState;

        public RoutineDescriptor Routine;

        public IValueContainer RoutineState;

        public TaskResult RoutineResult;

        /// <summary>
        /// If not null, the save state is caused due to calling another
        /// routine which must resume the current one upon completion.
        /// </summary>
        public ExecuteRoutineIntent AwaitedRoutine;
    }
}
