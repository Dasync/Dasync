using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.Serialization;

namespace Dasync.Fabric.InMemory
{
    public interface IInMemoryFabricSerializerFactoryAdvisor
    {
        ISerializerFactory Advise();
    }

    public class InMemoryFabricSerializerFactoryAdvisor : IInMemoryFabricSerializerFactoryAdvisor
    {
        private readonly ISerializerFactory _factory;

        public InMemoryFabricSerializerFactoryAdvisor(IEnumerable<ISerializerFactory> factories)
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
