using System;

namespace Dasync.AzureStorage
{
    public class TableRowAlreadyExistsException : Exception
    {
        public TableRowAlreadyExistsException() { }

        public TableRowAlreadyExistsException(string message) : base(message) { }
    }
}
