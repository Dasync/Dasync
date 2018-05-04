using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.AzureStorage
{
    public class CloudQueueClientAdapter : ICloudQueueClient
    {
        private readonly CloudQueueClient _client;

        public CloudQueueClientAdapter(CloudQueueClient client)
        {
            _client = client;
        }

        public ICloudQueue GetQueueReference(string queueName)
        {
            var queue = _client.GetQueueReference(queueName);
            return new CloudQueueAdapter(queue);
        }
    }
}
