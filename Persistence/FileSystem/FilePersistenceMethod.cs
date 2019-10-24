using System.IO;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Persistence.FileSystem
{
    public class FilePersistenceMethod : IPersistenceMethod
    {
        public const string MethodType = "File";

        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializerProvider;

        public FilePersistenceMethod(
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider)
        {
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializerProvider = serializerProvider;
        }

        public string Type => MethodType;

        public IMethodStateStorage CreateMethodStateStorage(IConfiguration configuration)
        {
            var baseDirectory = Path.GetFullPath(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "data"));
            var stateDirectory = Path.Combine(baseDirectory, "state");
            var resultsDirectory = Path.Combine(baseDirectory, "results");

            return new FileStorage(
                _defaultSerializer,
                _serializerProvider,
                stateDirectory,
                resultsDirectory);
        }
    }
}
