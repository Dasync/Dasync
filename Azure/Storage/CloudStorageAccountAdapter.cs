using Microsoft.WindowsAzure.Storage;

namespace Dasync.AzureStorage
{
    public class CloudStorageAccountAdapter : ICloudStorageAccount
    {
        private readonly CloudStorageAccount _account;

        public CloudStorageAccountAdapter(CloudStorageAccount account)
        {
            _account = account;
            TableClient = new CloudTableClientAdapter(account.CreateCloudTableClient());
            QueueClient = new CloudQueueClientAdapter(account.CreateCloudQueueClient());
        }

        public ICloudTableClient TableClient { get; }

        public ICloudQueueClient QueueClient { get; }
    }

    public class CloudStorageAccountFactory : ICloudStorageAccountFactory
    {
        public ICloudStorageAccount Create(string connectionString)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            return new CloudStorageAccountAdapter(account);
        }
    }
}
