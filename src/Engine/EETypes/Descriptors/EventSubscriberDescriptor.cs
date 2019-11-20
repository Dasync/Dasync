namespace Dasync.EETypes.Descriptors
{
    public class EventSubscriberDescriptor
    {
        public ServiceId Service { get; set; }

        public MethodId Method { get; set; }

        public override bool Equals(object obj) =>
            (obj is EventSubscriberDescriptor subscriber)
            ? this == subscriber
            : base.Equals(obj);

        public override int GetHashCode() =>
            (Service != null && Method != null)
            ? Service.GetHashCode() ^ Method.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(EventSubscriberDescriptor a, EventSubscriberDescriptor b) =>
            a.Service == b.Service && a.Method == b.Method;

        public static bool operator !=(EventSubscriberDescriptor a, EventSubscriberDescriptor b) => !(a == b);
    }
}
