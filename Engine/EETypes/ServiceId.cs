namespace Dasync.EETypes
{
    public sealed class ServiceId
    {
        public string ServiceName;

        // [FUTURE IDEA]
        // Can identify a service instance (a 'workflow'?) and
        // help service mesh to resolve dependencies and routing.
        //public string Distinguisher { get; set; }

        // [FUTURE IDEA]
        // Parent Service ID for service mesh? Helps to resolve
        // and route requests to proper service instances when
        // a service instance is created with a factory.
        // If so, who is considered a 'parent'?
        //public ServiceId ParentId;
    }
}
