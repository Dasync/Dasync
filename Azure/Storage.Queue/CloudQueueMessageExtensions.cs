using System;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.Azure.Storage.Queue
{
    public static class CloudQueueMessageExtensions
    {
#warning Pre-compile CloudQueueMessage extension methods

        public static CloudQueueMessage SetId(
            this CloudQueueMessage message,
            string id)
        {
            typeof(CloudQueueMessage)
                .GetProperty(nameof(CloudQueueMessage.Id))
                .SetValue(message, id);
            return message;
        }

        public static CloudQueueMessage SetPopReceipt(
            this CloudQueueMessage message,
            string popReceipt)
        {
            typeof(CloudQueueMessage)
                .GetProperty(nameof(CloudQueueMessage.PopReceipt))
                .SetValue(message, popReceipt);
            return message;
        }

        public static CloudQueueMessage SetDequeueCount(
            this CloudQueueMessage message,
            int dequeueCount)
        {
            typeof(CloudQueueMessage)
                .GetProperty(nameof(CloudQueueMessage.DequeueCount))
                .SetValue(message, dequeueCount);
            return message;
        }

        public static CloudQueueMessage SetInsertionTime(
            this CloudQueueMessage message,
            DateTimeOffset insertionTime)
        {
            typeof(CloudQueueMessage)
                .GetProperty(nameof(CloudQueueMessage.InsertionTime))
                .SetValue(message, insertionTime);
            return message;
        }

        public static CloudQueueMessage SetNextVisibleTime(
            this CloudQueueMessage message,
            DateTimeOffset nextVisibleTime)
        {
            typeof(CloudQueueMessage)
                .GetProperty(nameof(CloudQueueMessage.NextVisibleTime))
                .SetValue(message, nextVisibleTime);
            return message;
        }

        public static CloudQueueMessage SetExpirationTime(
            this CloudQueueMessage message,
            DateTimeOffset expirationTime)
        {
            typeof(CloudQueueMessage)
                .GetProperty(nameof(CloudQueueMessage.ExpirationTime))
                .SetValue(message, expirationTime);
            return message;
        }
    }
}
