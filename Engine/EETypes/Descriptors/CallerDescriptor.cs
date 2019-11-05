namespace Dasync.EETypes.Descriptors
{
    public class CallerDescriptor
    {
        public CallerDescriptor() { }

        public CallerDescriptor(ServiceId service, MethodId method, string intentId)
        {
            Service = service;
            Method = method;
            IntentId = intentId;
        }

        public CallerDescriptor(ServiceId service, EventId @event, string intentId)
        {
            Service = service;
            Event = @event;
            IntentId = intentId;
        }

        public CallerDescriptor(ServiceId service, MethodId method, EventId @event, string intentId)
        {
            Service = service;
            Method = method;
            Event = @event;
            IntentId = intentId;
        }

        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public EventId Event { get; set; }

        public string IntentId { get; set; }

        public CallerDescriptor Clone() =>
            new CallerDescriptor
            {
                Service = Service?.Clone(),
                Method = Method?.Clone(),
                Event = Event?.Clone(),
                IntentId = IntentId
            };
    }
}
