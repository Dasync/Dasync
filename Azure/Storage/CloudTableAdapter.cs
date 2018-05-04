using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dasync.AzureStorage
{
    public class CloudTableAdapter : ICloudTable
    {
        private readonly CloudTable _table;
        private readonly TableRequestOptions _options;

        public CloudTableAdapter(CloudTable table)
        {
            _table = table;
        }

        public string Name => _table.Name;

        public Task CreateAsync(CancellationToken cancellationToken)
        {
            return _table.CreateIfNotExistsAsync(null, null, cancellationToken);
        }

        public async Task InsertAsync(ITableEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _table.ExecuteAsync(
                    TableOperation.Insert(entity),
                    _options, null, cancellationToken);

                return;
            }
            catch (StorageException se)
            {
                if (se.RequestInformation?.HttpStatusCode == 409)
                    throw new TableRowAlreadyExistsException();
                HandleException(se);
                throw;
            }
        }

        public async Task ReplaceAsync(ITableEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _table.ExecuteAsync(
                    TableOperation.Replace(entity),
                    _options, null, cancellationToken);

                return;
            }
            catch (StorageException se)
            {
                if (se.RequestInformation?.HttpStatusCode == 412)
                    throw new TableRowETagMismatchException();
                HandleException(se);
                throw;
            }
        }

        public async Task InsertOrReplaceAsync(ITableEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _table.ExecuteAsync(
                    TableOperation.InsertOrReplace(entity),
                    _options, null, cancellationToken);

                return;
            }
            catch (StorageException se)
            {
                if (se.RequestInformation?.HttpStatusCode == 412)
                    throw new TableRowETagMismatchException();
                HandleException(se);
                throw;
            }
        }

        public async Task<T> TryRetrieveAsync<T>(
            string partitionKey,
            string rowKey,
            List<string> properties,
            CancellationToken cancellationToken)
            where T : ITableEntity
        {
            try
            {
                var result = await _table.ExecuteAsync(
                    TableOperation.Retrieve<T>(partitionKey, rowKey, properties),
                    _options, null, cancellationToken);

                if (result.Result != null)
                    return (T)result.Result;
            }
            catch (StorageException se)
            {
                HandleException(se);
                throw;
            }

            return default(T);
        }

        public async Task<List<T>> RetrieveAllAsync<T>(
            List<string> properties,
            CancellationToken cancellationToken)
            where T : ITableEntity, new()
        {
            try
            {
                var query = new TableQuery<T>();
                if (properties != null)
                    query = query.Select(properties);

                var continuationToken = new TableContinuationToken();
                var results = new List<T>();

                while (continuationToken != null)
                {
                    var segment = await _table.ExecuteQuerySegmentedAsync<T>(
                        query, continuationToken, _options, null, cancellationToken);

                    results.AddRange(segment.Results);

                    continuationToken = segment.ContinuationToken;
                }

                return results;
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
                    case 404: throw new TableDoesNotExistException(_table.Name, ex);
                }
            }
        }
    }
}
