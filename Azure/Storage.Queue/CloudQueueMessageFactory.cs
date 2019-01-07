using System;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.Azure.Storage.Queue
{
    public static class CloudQueueMessageFactory
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
    }
}
