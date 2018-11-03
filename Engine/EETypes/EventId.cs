namespace Dasync.EETypes
{
    public class EventId
    {
        /// <summary>
        /// The name of the event which is extracted with reflection.
        /// </summary>
        public string EventName;

        public override bool Equals(object obj) =>
            (obj is EventId eventId)
            ? this == eventId
            : base.Equals(obj);

        public override int GetHashCode() =>
            (EventName != null)
            ? EventName.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(EventId a, EventId b) => string.Equals(a?.EventName, b?.EventName);

        public static bool operator !=(EventId a, EventId b) => !(a == b);
    }
}
