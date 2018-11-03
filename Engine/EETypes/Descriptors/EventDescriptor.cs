namespace Dasync.EETypes.Descriptors
{
    public struct EventDescriptor
    {
        public ServiceId ServiceId;

        public EventId EventId;

        public override bool Equals(object obj) =>
            (obj is EventDescriptor eventDesc)
            ? this == eventDesc
            : base.Equals(obj);

        public override int GetHashCode() =>
            (ServiceId != null && EventId != null)
            ? ServiceId.GetHashCode() ^ EventId.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(EventDescriptor a, EventDescriptor b) =>
            a.ServiceId == b.ServiceId && a.EventId == b.EventId;

        public static bool operator !=(EventDescriptor a, EventDescriptor b) => !(a == b);
    }
}
