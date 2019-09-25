namespace Dasync.EETypes
{
    public class EventId
    {
        /// <summary>
        /// The name of the event which is extracted with reflection.
        /// </summary>
        public string Name;

        public override bool Equals(object obj) =>
            (obj is EventId eventId)
            ? this == eventId
            : base.Equals(obj);

        public override int GetHashCode() =>
            (Name != null)
            ? Name.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(EventId a, EventId b) => string.Equals(a?.Name, b?.Name);

        public static bool operator !=(EventId a, EventId b) => !(a == b);
    }
}
