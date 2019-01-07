using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.Azure.Storage.Queue
{
    public class CloudQueueProxy : ICloudQueue
    {
        private readonly CloudQueue _queue;
        private readonly QueueRequestOptions _options;

        public CloudQueueProxy(CloudQueue queue)
        {
            _queue = queue;
        }

        public string Name => _queue.Name;

        public Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return _queue.ExistsAsync(_options, null, cancellationToken);
        }

        public Task CreateAsync(CancellationToken cancellationToken)
        {
            return _queue.CreateAsync(_options, null, cancellationToken);
        }

        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _queue.DeleteAsync(_options, null, cancellationToken);
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }
        }

        public async Task AddMessageAsync(
            CloudQueueMessage message,
            TimeSpan? timeToLive,
            TimeSpan? initialVisibilityDelay,
            CancellationToken cancellationToken)
        {
            try
            {
                await _queue.AddMessageAsync(
                    message,
                    timeToLive,
                    initialVisibilityDelay,
                    _options,
                    null,
                    cancellationToken);
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }
        }

        public async Task<CloudQueueMessage> GetMessageAsync(
            TimeSpan? visibilityTimeout,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _queue.GetMessageAsync(
                    visibilityTimeout,
                    _options,
                    null,
                    cancellationToken);
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }
        }

        public async Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(
            int messageCount,
            TimeSpan? visibilityTimeout,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _queue.GetMessagesAsync(
                    messageCount,
                    visibilityTimeout,
                    _options,
                    null,
                    cancellationToken);
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }
        }

        public async Task UpdateMessageAsync(
            CloudQueueMessage message,
            TimeSpan visibilityTimeout,
            MessageUpdateFields updateFields,
            CancellationToken cancellationToken)
        {
            try
            {
                await _queue.UpdateMessageAsync(
                    message,
                    visibilityTimeout,
                    updateFields,
                    _options,
                    null,
                    cancellationToken);
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }
        }

        public async Task DeleteMessageAsync(
            string messageId,
            string popReceipt,
            CancellationToken cancellationToken)
        {
            try
            {
                await _queue.DeleteMessageAsync(
                    messageId,
                    popReceipt,
                    _options,
                    null,
                    cancellationToken);
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }
        }

        private void HandleException(StorageException ex)
        {
            if (ex.RequestInformation != null)
            {
                switch (ex.RequestInformation.HttpStatusCode)
                {
                    case 404: throw new QueueDoesNotExistException(_queue.Name, ex);
                }
            }
        }
    }
}
