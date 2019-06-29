using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.Azure.Storage.Queue
{
    public interface ICloudQueue
    {
        string Name { get; }

        StorageUri StorageUri { get; }

        Task<bool> ExistsAsync(CancellationToken cancellationToken);

        Task CreateAsync(CancellationToken cancellationToken);

        Task DeleteAsync(CancellationToken cancellationToken);

        Task AddMessageAsync(
            CloudQueueMessage message,
            TimeSpan? timeToLive,
            TimeSpan? initialVisibilityDelay,
            CancellationToken cancellationToken);

        Task<CloudQueueMessage> GetMessageAsync(
            TimeSpan? visibilityTimeout,
            CancellationToken cancellationToken);

        Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(
            int messageCount,
            TimeSpan? visibilityTimeout,
            CancellationToken cancellationToken);

        Task UpdateMessageAsync(
            CloudQueueMessage message,
            TimeSpan visibilityTimeout,
            MessageUpdateFields updateFields,
            CancellationToken cancellationToken);

        Task DeleteMessageAsync(
            string messageId,
            string popReceipt,
            CancellationToken cancellationToken);
    }
}
