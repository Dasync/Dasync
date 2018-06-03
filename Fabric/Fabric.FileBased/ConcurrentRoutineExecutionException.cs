using System;

namespace Dasync.Fabric.FileBased
{
    public class ConcurrentRoutineExecutionException : Exception
    {
        private const string DefaultMessage = "The routine has been executed concurrently.";

        public ConcurrentRoutineExecutionException()
            : base(DefaultMessage)
        {
        }

        public ConcurrentRoutineExecutionException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }

        public ConcurrentRoutineExecutionException(string message)
            : base(message)
        {
        }

        public ConcurrentRoutineExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
