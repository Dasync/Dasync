using Microsoft.WindowsAzure.Storage.Table;

namespace Dasync.AzureStorage
{
    public class CloudTableClientAdapter : ICloudTableClient
    {
        private readonly CloudTableClient _client;

        public CloudTableClientAdapter(CloudTableClient client)
        {
            _client = client;
        }

        public ICloudTable GetTableReference(string tableName)
        {
            var table = _client.GetTableReference(tableName);
            return new CloudTableAdapter(table);
        }
    }
}
