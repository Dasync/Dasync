using System;

namespace Dasync.EETypes.Descriptors
{
    public sealed class ContinuationDescriptor
    {
        public ServiceId ServiceId;

        public RoutineDescriptor Routine;

        public DateTime? ContinueAt;

#warning Add state of the actual routine being resumed? That option would remove the need of persistant storage for the state - eveything is conveyed in messages. However, that can blow the size of a message - need overflow mechanism.
    }
}
