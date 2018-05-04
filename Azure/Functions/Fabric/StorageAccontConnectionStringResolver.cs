using System;
using Dasync.AzureStorage;
using Dasync.FabricConnector.AzureStorage;

namespace Dasync.Fabric.AzureFunctions
{
    public class StorageAccontConnectionStringResolver : IStorageAccontConnectionStringResolver
    {
        private readonly IAzureWebJobsEnviromentalSettings _azureWebJobsEnviromentalSettings;

        public StorageAccontConnectionStringResolver(
            IAzureWebJobsEnviromentalSettings azureWebJobsEnviromentalSettings)
        {
            _azureWebJobsEnviromentalSettings = azureWebJobsEnviromentalSettings;
        }

        public string Resolve(string accountName)
        {
            var defaultConnectionString = _azureWebJobsEnviromentalSettings.DefaultStorageConnectionString;
            var defaultAccountName = ConnectionStringParser.GetAccountName(defaultConnectionString);
            if (string.Equals(accountName, defaultAccountName))
                return defaultConnectionString;

            if (_azureWebJobsEnviromentalSettings.TryGetSetting(
                key: accountName + "_STORAGE",
                value: out var connectionString))
                return connectionString;

            throw new InvalidOperationException(
                $"Could not resolve connection string for a storage account '{accountName}'.");
        }
    }
}
