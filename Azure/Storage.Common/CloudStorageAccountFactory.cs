using Microsoft.WindowsAzure.Storage;

namespace Dasync.Azure.Storage.Common
{
    internal class CloudStorageAccountFactory : ICloudStorageAccountFactory
    {
        public ICloudStorageAccount Create(string connectionString)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            return new CloudStorageAccountProxy(account);
        }
    }
}
