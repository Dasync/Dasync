using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Dasync.Azure.Storage.Common
{
    public interface ICloudStorageAccount
    {
        StorageUri FileStorageUri { get; }

        StorageUri BlobStorageUri { get; }

        StorageUri QueueStorageUri { get; }

        StorageUri TableStorageUri { get; }

        StorageCredentials Credentials { get; }

        string GetSharedAccessSignature(SharedAccessAccountPolicy policy);

        string ToString(bool exportSecrets);
    }
}
