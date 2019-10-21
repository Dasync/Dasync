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

        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public string IntentId { get; set; }

        public CallerDescriptor Clone() =>
            new CallerDescriptor
            {
                Service = Service.Clone(),
                Method = Method.Clone(),
                IntentId = IntentId
            };
    }
}
