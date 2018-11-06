namespace Dasync.EETypes
{
    public sealed class ServiceId
    {
        public string ServiceName;

        /// <summary>
        /// A name of a proxy service that performs the actual work.
        /// </summary>
        /// <remarks>
        /// This is a quick fix for IntrinsicRoutines, where ServiceName
        /// needs to be used to select the connector (route requests).
        /// </remarks>
        public string ProxyName;

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

        public override bool Equals(object obj) =>
            (obj is ServiceId serviceId)
            ? this == serviceId
            : base.Equals(obj);

        public override int GetHashCode() =>
            (ServiceName != null)
            ? ServiceName.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(ServiceId a, ServiceId b) => string.Equals(a?.ServiceName, b?.ServiceName);

        public static bool operator !=(ServiceId a, ServiceId b) => !(a == b);
    }
}
