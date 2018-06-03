using System;

namespace Dasync.Fabric.FileBased
{
    public class ETagMismatchException : Exception
    {
        private const string DefaultMessage = "The ETag did not match, what indicates a race condition.";

        public ETagMismatchException()
            : base(DefaultMessage)
        {
        }

        public ETagMismatchException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }

        public ETagMismatchException(string message)
            : base(message)
        {
        }

        public ETagMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
