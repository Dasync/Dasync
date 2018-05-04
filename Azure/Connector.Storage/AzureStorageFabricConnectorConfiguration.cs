namespace Dasync.FabricConnector.AzureStorage
{
    public class AzureStorageFabricConnectorConfiguration
    {
        public string StorageAccountName { get; set; }

        public string TransitionsQueueName { get; set; }

        public string RoutinesTableName { get; set; }

        public string ServicesTableName { get; set; }

        public string SerializerFormat { get; set; }
    }
}
