using System;

namespace Dasync.Azure.Storage.Queue
{
    public class QueueDoesNotExistException : Exception
    {
        public QueueDoesNotExistException(string queueName)
            : this(queueName, null)
        {
        }

        public QueueDoesNotExistException(string queueName, Exception innerException)
            : base ($"The queue '{queueName}' does not exist", innerException)
        {
            QueueName = queueName;
        }

        public string QueueName { get; }
    }
}
