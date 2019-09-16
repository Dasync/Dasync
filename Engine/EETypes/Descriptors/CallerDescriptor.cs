namespace Dasync.EETypes.Descriptors
{
    public class CallerDescriptor
    {
        public CallerDescriptor() { }

        public CallerDescriptor(ServiceId service, RoutineMethodId routine, string intentId)
        {
            Service = service;
            Routine = routine;
            IntentId = intentId;
        }

        public ServiceId Service;

        public RoutineMethodId Routine;

        public string IntentId;
    }
}
