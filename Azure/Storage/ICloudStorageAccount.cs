namespace Dasync.AzureStorage
{
    public interface ICloudStorageAccount
    {
        ICloudTableClient TableClient { get; }

        ICloudQueueClient QueueClient { get; }
    }

    public interface ICloudStorageAccountFactory
    {
        ICloudStorageAccount Create(string connectionString);
    }
}
