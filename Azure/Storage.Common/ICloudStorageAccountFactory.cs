namespace Dasync.Azure.Storage.Common
{
    public interface ICloudStorageAccountFactory
    {
        ICloudStorageAccount Create(string connectionString);
    }
}
