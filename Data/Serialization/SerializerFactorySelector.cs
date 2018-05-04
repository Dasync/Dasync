using System;
using System.Collections.Generic;

namespace Dasync.Serialization
{
    public class SerializerFactorySelector : ISerializerFactorySelector
    {
        private readonly Dictionary<string, ISerializerFactory> _factoryMap;

        public SerializerFactorySelector(ISerializerFactory[] factories)
        {
            _factoryMap = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
            foreach (var factory in factories)
                _factoryMap.Add(factory.Format, factory);
        }

        public ISerializerFactory Select(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException(nameof(format));

            if (_factoryMap.TryGetValue(format, out var factory))
                return factory;

            throw new NotSupportedException($"No factory for a serializer of format '{format}'.");
        }
    }
}
