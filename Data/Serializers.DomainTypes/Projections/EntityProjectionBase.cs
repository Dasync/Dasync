using System.Diagnostics;

namespace Dasync.Serializers.DomainTypes.Projections
{
    public abstract class EntityProjectionBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _tag;

        public void SetTag(object tag) => _tag = tag;

        public object GetTag() => _tag;
    }
}
