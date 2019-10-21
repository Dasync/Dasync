using System;

namespace Dasync.EETypes.Persistence
{
    public class ETagMismatchException : Exception
    {
        public ETagMismatchException() : base("The ETag does not match") { }

        public ETagMismatchException(string expectedETag, string actualETag) : this()
        {
            ExpectedETag = expectedETag;
            ActualETag = actualETag;
        }

        public string ExpectedETag { get; }

        public string ActualETag { get; }
    }
}
