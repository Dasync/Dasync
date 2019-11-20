using System;
using System.Diagnostics;

namespace Dasync.EETypes
{
    /// <summary>
    /// Uniquely identifies a routine method.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class MethodId
    {
        public string Name { get; set; }

#warning Generic parameters
#warning Method signature hash?
#warning Does it matter if it's an interface method or not?
#warning Method/client version?

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return Name;
            }
        }

        public override bool Equals(object obj) =>
            (obj is MethodId methodId)
            ? this == methodId
            : base.Equals(obj);

        public override int GetHashCode() =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? "");

        public static bool operator ==(MethodId a, MethodId b) =>
            string.Equals(a?.Name, b?.Name, System.StringComparison.OrdinalIgnoreCase);

        public static bool operator !=(MethodId a, MethodId b) => !(a == b);

        public void Deconstruct(out string name)
        {
            name = Name;
        }

        public virtual MethodId Clone() => CopyTo(new MethodId());

        public virtual T CopyTo<T>(T copy) where T : MethodId
        {
            copy.Name = Name;
            return copy;
        }
    }
}
