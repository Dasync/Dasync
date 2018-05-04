using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dasync.AzureStorage
{
    public interface ICloudTable
    {
        string Name { get; }

        Task CreateAsync(CancellationToken cancellationToken);

        Task InsertAsync(ITableEntity entity, CancellationToken cancellationToken);

        Task ReplaceAsync(ITableEntity entity, CancellationToken cancellationToken);

        Task InsertOrReplaceAsync(ITableEntity entity, CancellationToken cancellationToken);

        Task<T> TryRetrieveAsync<T>(
            string partitionKey,
            string rowKey,
            List<string> properties,
            CancellationToken cancellationToken)
            where T : ITableEntity;

        Task<List<T>> RetrieveAllAsync<T>(
            List<string> properties,
            CancellationToken cancellationToken)
            where T : ITableEntity, new();
    }
}
