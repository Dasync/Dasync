namespace Dasync.EETypes.Descriptors
{
    public sealed class ServiceDescriptor
    {
        public ServiceId Id { get; set; }

        public string[] Interfaces { get; set; }
    }
}
