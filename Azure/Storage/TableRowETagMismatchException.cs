using System;

namespace Dasync.AzureStorage
{
    public class TableRowETagMismatchException : Exception
    {
        public TableRowETagMismatchException() { }

        public TableRowETagMismatchException(string message) : base(message) { }
    }
}
