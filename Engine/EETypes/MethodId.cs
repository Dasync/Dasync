namespace Dasync.EETypes
{
    /// <summary>
    /// Uniquely identifies a routine method.
    /// </summary>
    public class MethodId
    {
        public string Name { get; set; }

#warning Generic parameters
#warning Method signature hash?
#warning Does it matter if it's an interface method or not?
#warning Method/client version?

        public override bool Equals(object obj) =>
            (obj is MethodId methodId)
            ? this == methodId
            : base.Equals(obj);

        public override int GetHashCode() =>
            (Name != null)
            ? Name.GetHashCode()
            : base.GetHashCode();

        public static bool operator ==(MethodId a, MethodId b) =>
            string.Equals(a?.Name, b?.Name, System.StringComparison.OrdinalIgnoreCase);

        public static bool operator !=(MethodId a, MethodId b) => !(a == b);

        public void Deconstruct(out string name)
        {
            name = Name;
        }

        public MethodId Clone() => CopyTo(new MethodId());

        public T CopyTo<T>(T copy) where T : MethodId
        {
            copy.Name = Name;
            return copy;
        }
    }
}
