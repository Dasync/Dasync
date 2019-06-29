using System.Collections.Specialized;
using System.Threading;

namespace Dasync.AspNetCore
{
    public interface ITransitionUserContext
    {
        NameValueCollection Current { get; set; }
    }

    internal class TransitionUserContext : ITransitionUserContext
    {
        private class ContextHolder
        {
            public NameValueCollection Context;
        }

        private static readonly AsyncLocal<ContextHolder> _currentContext = new AsyncLocal<ContextHolder>();

        public NameValueCollection Current
        {
            get => _currentContext.Value?.Context;
            set
            {
                var holder = _currentContext.Value;
                if (holder == null)
                {
                    holder = new ContextHolder();
                    _currentContext.Value = holder;
                }
                holder.Context = value;
            }
        }
    }
}
