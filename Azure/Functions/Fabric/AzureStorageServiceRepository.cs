using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Dasync.FabricConnector.AzureStorage;
using Dasync.ServiceRegistry;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Dasync.Fabric.AzureFunctions
{
    public class AzureStorageServiceRepository : IServiceDiscovery, IServicePublisher
    {
        private readonly List<ICloudTable> _registryTables = new List<ICloudTable>();

        public AzureStorageServiceRepository(
            ICloudStorageAccountFactory cloudStorageAccountFactory,
            IAzureWebJobsEnviromentalSettings azureWebJobsEnviromentalSettings)
        {
            var accountNames = new HashSet<string>();

            var defaultAccountName = ConnectionStringParser.GetAccountName(
                azureWebJobsEnviromentalSettings.DefaultStorageConnectionString);
            accountNames.Add(defaultAccountName);

            var defaultStorageAccount = cloudStorageAccountFactory.Create(
                azureWebJobsEnviromentalSettings.DefaultStorageConnectionString);
            var registryTable = defaultStorageAccount.TableClient.GetTableReference("registry");
            _registryTables.Add(registryTable);

            foreach (var setting in azureWebJobsEnviromentalSettings.GetAllSettings())
            {
                if (!setting.Key.EndsWith("_STORAGE"))
                    continue;

                var connectionString = setting.Value;
                var accountName = ConnectionStringParser.GetAccountName(connectionString);
                if (!accountNames.Add(accountName))
                    continue;

                var storageAccount = cloudStorageAccountFactory.Create(connectionString);
                registryTable = storageAccount.TableClient.GetTableReference("registry");
                _registryTables.Add(registryTable);
            }
        }

        private static string NormalizeConnectionString(string connectionString)
        {
            return connectionString;
        }

        public async Task<IEnumerable<ServiceRegistrationInfo>> DiscoverAsync(CancellationToken ct)
        {
            var result = new List<ServiceRegistrationInfo>();
            foreach (var registryTable in _registryTables)
            {
#warning de-duplicate records - self-hosted services get populated everywhere.
                List<ServiceRegistryRecord> records;
                try
                {
                    records = await registryTable.RetrieveAllAsync<ServiceRegistryRecord>(null, ct);
                }
                catch(TableDoesNotExistException)
                {
                    continue;
                }
                result.AddRange(records.Select(Convert));
            }
            return result;
        }

        public async Task PublishAsync(IEnumerable<ServiceRegistrationInfo> services, CancellationToken ct)
        {
            foreach (var registryTable in _registryTables)
            {
                foreach (var serviceInfo in services)
                {
                    var record = Convert(serviceInfo);
                    while (true)
                    {
                        try
                        {
                            await registryTable.InsertOrReplaceAsync(record, ct);
                            break;
                        }
                        catch (TableDoesNotExistException)
                        {
                            await registryTable.CreateAsync(ct);
                        }
                    }
                }
            }
        }

        private ServiceRegistrationInfo Convert(ServiceRegistryRecord record)
        {
#warning How to generalize connector configuration serialization?
            if (record.ConnectorType != "AzureStorage")
                throw new NotSupportedException($"Unsupported connector type '{record.ConnectorType}'.");

            var configuration = JsonConvert.DeserializeObject
                <AzureStorageFabricConnectorConfiguration>
                (record.ConnectorConfiguration);

            return new ServiceRegistrationInfo
            {
                Name = record.RowKey,
                IsExternal = true,
                IsSingleton = true,
                ConnectorType = record.ConnectorType,
                ConnectorConfiguration = configuration
            };
        }

        private ServiceRegistryRecord Convert(ServiceRegistrationInfo serviceInfo)
        {
            var configurationJson = JsonConvert.SerializeObject(serviceInfo.ConnectorConfiguration);

            return new ServiceRegistryRecord
            {
                PartitionKey = string.Empty,
                RowKey = serviceInfo.Name,
                ConnectorType = serviceInfo.ConnectorType,
                ConnectorConfiguration = configurationJson
            };
        }
    }

    public class ServiceRegistryRecord : TableEntity
    {
        public string ConnectorType { get; set; }

        public string ConnectorConfiguration { get; set; }
    }
}
