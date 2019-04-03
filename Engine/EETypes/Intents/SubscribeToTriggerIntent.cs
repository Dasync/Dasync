using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    public class SubscribeToTriggerIntent
    {
        public string TriggerId;

        public ContinuationDescriptor Continuation;
    }
}
