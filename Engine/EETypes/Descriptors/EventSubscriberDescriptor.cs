namespace Dasync.EETypes.Descriptors
{
    public class EventSubscriberDescriptor
    {
        public ServiceId ServiceId;
        public RoutineMethodId MethodId;

        public override bool Equals(object obj) =>
            (obj is EventSubscriberDescriptor subscriber)
            ? this == subscriber
            : base.Equals(obj);

        public override int GetHashCode() =>
            (ServiceId != null && MethodId != null)
            ? ServiceId.GetHashCode() ^ MethodId.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(EventSubscriberDescriptor a, EventSubscriberDescriptor b) =>
            a.ServiceId == b.ServiceId && a.MethodId == b.MethodId;

        public static bool operator !=(EventSubscriberDescriptor a, EventSubscriberDescriptor b) => !(a == b);
    }
}
