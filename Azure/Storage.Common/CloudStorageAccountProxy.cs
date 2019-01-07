using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Dasync.Azure.Storage.Common
{
    internal class CloudStorageAccountProxy : ICloudStorageAccount
    {
        private readonly CloudStorageAccount _account;

        public CloudStorageAccountProxy(CloudStorageAccount account)
        {
            _account = account;
        }

        public StorageUri FileStorageUri => _account.FileStorageUri;

        public StorageUri BlobStorageUri => _account.BlobStorageUri;

        public StorageUri QueueStorageUri => _account.QueueStorageUri;

        public StorageUri TableStorageUri => _account.TableStorageUri;

        public StorageCredentials Credentials => _account.Credentials;

        public string GetSharedAccessSignature(SharedAccessAccountPolicy policy) => _account.GetSharedAccessSignature(policy);

        public string ToString(bool exportSecrets) => _account.ToString(exportSecrets);
    }
}
