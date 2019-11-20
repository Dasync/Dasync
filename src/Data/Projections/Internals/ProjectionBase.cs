using System.Diagnostics;

namespace Dasync.Projections.Internals
{
    public abstract class ProjectionBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _tag;

        public void SetTag(object tag) => _tag = tag;

        public object GetTag() => _tag;
    }
}
