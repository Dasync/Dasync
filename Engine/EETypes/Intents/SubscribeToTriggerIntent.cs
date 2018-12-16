using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    public class SubscribeToTriggerIntent
    {
        public long TriggerId;

        public ContinuationDescriptor Continuation;
    }
}
