namespace Dasync.AzureStorage
{
    public interface ICloudQueueClient
    {
        ICloudQueue GetQueueReference(string queueName);
    }
}
