using System;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.AzureStorage
{
    public static class CloudQueueMessageExtensions
    {
        public static CloudQueueMessage Create(
            byte[] content,
            string id = null,
            string popReceipt = null,
            int? dequeueCount = null,
            DateTimeOffset? insertionTime = null,
            DateTimeOffset? nextVisibleTime = null,
            DateTimeOffset? expirationTime = null)
        {
            var message = new CloudQueueMessage(string.Empty);
            message.SetMessageContent(content);
            if (id != null)
                message.SetId(id);
            if (popReceipt != null)
                message.SetPopReceipt(popReceipt);
            if (dequeueCount.HasValue)
                message.SetDequeueCount(dequeueCount.Value);
            if (insertionTime.HasValue)
                message.SetInsertionTime(insertionTime.Value);
            if (nextVisibleTime.HasValue)
                message.SetNextVisibleTime(nextVisibleTime.Value);
            if (expirationTime.HasValue)
                message.SetExpirationTime(expirationTime.Value);
            return message;
        }

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
