namespace Dasync.EETypes
{
    /// <summary>
    /// Uniquely identifies a routine method.
    /// </summary>
    public sealed class RoutineMethodId
    {
        public string Name { get; set; }

#warning Generic parameters
#warning Method signature hash?
#warning Does it matter if it's an interface method or not?
#warning Method/client version?

        public override bool Equals(object obj) =>
            (obj is RoutineMethodId methodId)
            ? this == methodId
            : base.Equals(obj);

        public override int GetHashCode() =>
            (Name != null)
            ? Name.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(RoutineMethodId a, RoutineMethodId b) =>
            string.Equals(a?.Name, b?.Name, System.StringComparison.OrdinalIgnoreCase);

        public static bool operator !=(RoutineMethodId a, RoutineMethodId b) => !(a == b);

        public RoutineMethodId Copy() => new RoutineMethodId { Name = Name };
    }
}
