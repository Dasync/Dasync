using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Persistence.FileSystem
{
    public class FileStorage : IMethodStateStorage
    {
        private const int FileBufferSize = 8 * 1024;

        private readonly ISerializer _serializer;
        private readonly ISerializerProvider _serializerProvider;
        private readonly string _stateDirectory;
        private readonly string _resultsDirectory;

        public FileStorage(
            ISerializer serializer,
            ISerializerProvider serializerProvider,
            string stateDirectory,
            string resultsDirectory)
        {
            _serializer = serializer;
            _serializerProvider = serializerProvider;
            _stateDirectory = stateDirectory;
            _resultsDirectory = resultsDirectory;
        }

        public async Task<string> WriteStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            MethodExecutionState state)
        {
            var fileName = GetStateFileName(serviceId, methodId);
            var filePath = Path.Combine(_stateDirectory, fileName);

            var data = _serializer.SerializeToBytes(state);

            EnsureDirectoryExists(_stateDirectory);

        @TryWrite:
            var tryCount = 10;
            try
            {
                using (var fileStream = new FileStream(
                    filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                    FileBufferSize, FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    var etag = TryGetETag(filePath);
                    if (fileStream.Length > 0 && !string.IsNullOrEmpty(methodId.ETag) && methodId.ETag != etag)
                        throw new ETagMismatchException(methodId.ETag, etag);

                    await fileStream.WriteAsync(data, 0, data.Length);

                    fileStream.SetLength(fileStream.Position);

                    return etag;
                }
            }
            catch (IOException) when (tryCount > 0)
            {
                await Task.Yield();
                tryCount--;
                goto @TryWrite;
            }
        }

        public async Task<MethodExecutionState> ReadStateAsync(ServiceId serviceId, PersistedMethodId methodId, CancellationToken ct)
        {
            var fileName = GetStateFileName(serviceId, methodId);
            var filePath = Path.Combine(_stateDirectory, fileName);

            string etag;
            byte[] data;

            try
            {
                using (var fileStream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    FileBufferSize, FileOptions.Asynchronous))
                {
                    etag = TryGetETag(filePath);
                    data = new byte[fileStream.Length];
                    await fileStream.ReadAsync(data, 0, data.Length);
                }
            }
            catch (IOException)
            {
                throw new StateNotFoundException(serviceId, methodId);
            }

            // TODO: select serializer
            var executionState = _serializer.Deserialize<MethodExecutionState>(data);
            return executionState;
        }

        public async Task WriteResultAsync(ServiceId serviceId, MethodId methodId, string intentId, ITaskResult result)
        {
            var fileName = GetResultFileName(serviceId, methodId, intentId);
            var filePath = Path.Combine(_resultsDirectory, fileName);

            var serializedResult = _serializer.SerializeToBytes(result);

            EnsureDirectoryExists(_resultsDirectory);
            using (var fileStream = new FileStream(
                filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                FileBufferSize, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await fileStream.WriteAsync(serializedResult, 0, serializedResult.Length);
                fileStream.SetLength(fileStream.Position);
            }
        }

        public async Task<ITaskResult> TryReadResultAsync(ServiceId serviceId, MethodId methodId, string intentId, Type resultValueType, CancellationToken ct)
        {
            var fileName = GetResultFileName(serviceId, methodId, intentId);
            var filePath = Path.Combine(_resultsDirectory, fileName);

            if (!File.Exists(filePath))
                return null;

#if NETSTANDARD2_0
            var bytes = File.ReadAllBytes(filePath);
#else
            var bytes = await File.ReadAllBytesAsync(filePath);
#endif

            // TODO: select serializer
            var result = TaskResult.CreateEmpty(resultValueType);
            _serializer.Populate(bytes, (IValueContainer)result);
            return result;
        }

        private string GetStateFileName(ServiceId serviceId, PersistedMethodId methodId)
        {
            // TODO: suffix for the format? e.g. '.json', '.gz'
            return $"{methodId.IntentId}.{serviceId.Name}.{methodId.Name}.state";
        }

        private string GetResultFileName(ServiceId serviceId, MethodId methodId, string intentId)
        {
            // TODO: suffix for the format? e.g. '.json', '.gz'
            return $"{intentId}.{serviceId.Name}.{methodId.Name}.result";
        }

        private static string TryGetETag(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            return new DateTimeOffset(File.GetLastWriteTimeUtc(filePath)).ToString("o");
        }

        private static void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
