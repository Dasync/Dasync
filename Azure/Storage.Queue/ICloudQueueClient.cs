namespace Dasync.Azure.Storage.Queue
{
    public interface ICloudQueueClient
    {
        ICloudQueue GetQueueReference(string queueName);
    }
}
