using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Persistence.InMemory
{
    public class InMemoryPersistenceMethod : IPersistenceMethod
    {
        private IMethodStateStorage _singleStorage;

        public const string MethodType = "InMemory";

        public InMemoryPersistenceMethod(IDefaultSerializerProvider defaultSerializerProvider)
        {
            _singleStorage = new InMemoryMethodStateStorage(defaultSerializerProvider.DefaultSerializer);
        }

        public string Type => MethodType;

        public IMethodStateStorage CreateMethodStateStorage(IConfiguration configuration)
        {
            return _singleStorage;
        }
    }
}
