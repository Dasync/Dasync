using System;
using System.Linq;
using Dasync.Serialization;

namespace Dasync.Fabric.FileBased
{
    public interface IFileBasedFabricSerializerFactoryAdvisor
    {
        ISerializerFactory Advise();
    }

    public class FileBasedFabricSerializerFactoryAdvisor : IFileBasedFabricSerializerFactoryAdvisor
    {
        private readonly ISerializerFactory _factory;

        public FileBasedFabricSerializerFactoryAdvisor(ISerializerFactory[] factories)
        {
            _factory = factories?.FirstOrDefault();
        }

        public ISerializerFactory Advise()
        {
            if (_factory == null)
                throw new InvalidOperationException("No serializer factories have been registered.");
            return _factory;
        }
    }
}
