using System;

namespace Dasync.ExecutionEngine
{
    public class ConcurrentTransitionException : Exception
    {
        public ConcurrentTransitionException() : base() { }

        public ConcurrentTransitionException(Exception innerException) : base("", innerException) { }
    }
}
