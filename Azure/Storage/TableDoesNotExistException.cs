using System;

namespace Dasync.AzureStorage
{
    public class TableDoesNotExistException : Exception
    {
        public TableDoesNotExistException(string tableName)
            : this(tableName, null)
        {
        }

        public TableDoesNotExistException(string tableName, Exception innerException)
            : base($"The table '{tableName}' does not exist", innerException)
        {
            TableName = tableName;
        }

        public string TableName { get; }
    }
}
