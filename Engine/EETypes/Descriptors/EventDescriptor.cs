namespace Dasync.EETypes.Descriptors
{
    public struct EventDescriptor
    {
        public ServiceId Service { get; set; }

        public EventId Event { get; set; }

        public override bool Equals(object obj) =>
            (obj is EventDescriptor eventDesc)
            ? this == eventDesc
            : base.Equals(obj);

        public override int GetHashCode() =>
            (Service != null && Event != null)
            ? Service.GetHashCode() ^ Event.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(EventDescriptor a, EventDescriptor b) =>
            a.Service == b.Service && a.Event == b.Event;

        public static bool operator !=(EventDescriptor a, EventDescriptor b) => !(a == b);
    }
}
