using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.Azure.Storage.Queue
{
    public class CloudQueueClientProxy : ICloudQueueClient
    {
        private readonly CloudQueueClient _client;

        public CloudQueueClientProxy(CloudQueueClient client)
        {
            _client = client;
        }

        public ICloudQueue GetQueueReference(string queueName)
        {
            var queue = _client.GetQueueReference(queueName);
            return new CloudQueueProxy(queue);
        }
    }
}
