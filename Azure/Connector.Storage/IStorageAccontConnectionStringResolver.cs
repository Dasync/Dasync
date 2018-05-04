namespace Dasync.FabricConnector.AzureStorage
{
    /// <summary>
    /// Used to hide secrects from service registry and resolve the connection string
    /// from a <see cref="AzureStorageFabricConnectorConfiguration.StorageAccountName"/>.
    /// </summary>
    public interface IStorageAccontConnectionStringResolver
    {
        string Resolve(string accountName);
    }
}
