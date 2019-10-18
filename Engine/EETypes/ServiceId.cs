using System.Diagnostics;

namespace Dasync.EETypes
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class ServiceId
    {
        public string Name { get; set; }

        /// <summary>
        /// A name of a proxy service that performs the actual work.
        /// </summary>
        /// <remarks>
        /// This is a quick fix for IntrinsicRoutines, where ServiceName
        /// needs to be used to select the connector (route requests).
        /// </remarks>
        public string Proxy { get; set; }

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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Proxy))
                    return Name;
                return $"{Name} ({Proxy})";
            }
        }

        public override bool Equals(object obj) =>
            (obj is ServiceId serviceId)
            ? this == serviceId
            : base.Equals(obj);

        public override int GetHashCode() =>
            (Name != null)
            ? Name.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(ServiceId a, ServiceId b) => string.Equals(a?.Name, b?.Name);

        public static bool operator !=(ServiceId a, ServiceId b) => !(a == b);

        public void Deconstruct(out string name, out string proxy)
        {
            name = Name;
            proxy = Proxy;
        }

        public ServiceId Clone() => CopyTo(new ServiceId());

        public T CopyTo<T>(T copy) where T : ServiceId
        {
            copy.Name = Name;
            copy.Proxy = Proxy;
            return copy;
        }
    }
}
