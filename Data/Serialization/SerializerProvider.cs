using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Serialization
{
    public interface ISerializerProvider
    {
        ISerializer GetSerializer(string contentType);
    }

    internal class SerializerProvider : ISerializerProvider
    {
        private readonly Dictionary<string, Lazy<ISerializer>> _serializers;

        public SerializerProvider(IEnumerable<ISerializerFactory> factories)
        {
            _serializers = factories.ToDictionary(
                f => f.Format,
                f => new Lazy<ISerializer>(() => f.Create(), isThreadSafe: true),
                StringComparer.OrdinalIgnoreCase);
        }

        public ISerializer GetSerializer(string contentType)
        {
            if (!_serializers.TryGetValue(contentType, out var serializer))
                throw new ArgumentException($"No serializer found for '{contentType}'.", nameof(contentType));
            return serializer.Value;
        }
    }

    public interface IDefaultSerializerProvider
    {
        ISerializer DefaultSerializer { get; }
    }

    internal class DefaultSerializerProvider : IDefaultSerializerProvider
    {
        private readonly ISerializerProvider _serializerProvider;

        public DefaultSerializerProvider(ISerializerProvider serializerProvider) =>
            _serializerProvider = serializerProvider;

        public ISerializer DefaultSerializer =>
            // TODO: use options
            _serializerProvider.GetSerializer("dasync+json");
    }
}
