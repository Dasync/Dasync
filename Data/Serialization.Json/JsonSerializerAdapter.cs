using System;
using System.IO;
using Dasync.ValueContainer;
using Newtonsoft.Json;

namespace Dasync.Serialization.Json
{
    public class JsonSerializerAdapter : ISerializer, ITextSerializer
    {
        private readonly JsonSerializer _serializer;

        public JsonSerializerAdapter(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public string Format => JsonSerializerAdapterFactory.FormatName;

        public void Serialize(Stream stream, object @object, Type objectType = null)
        {
            using (var writer = new StreamWriter(stream, Encodings.UTF8, 1024, leaveOpen: true))
                Serialize(stream, @object, objectType);
        }

        public void Serialize(TextWriter writer, object @object, Type objectType = null)
        {
            _serializer.Serialize(writer, @object, objectType);
        }

        public void Populate(Stream stream, IValueContainer target)
        {
            using (var reader = new StreamReader(stream, Encodings.UTF8, true, 1024, leaveOpen: true))
                Populate(reader, target);
        }

        public void Populate(TextReader reader, IValueContainer target)
        {
            _serializer.Populate(reader, target);
        }

        public object Deserialize(Stream stream, Type objectType = null)
        {
            using (var reader = new StreamReader(stream, Encodings.UTF8, true, 1024, leaveOpen: true))
                return Deserialize(reader, objectType);
        }

        public object Deserialize(TextReader reader, Type objectType = null)
        {
            return _serializer.Deserialize(reader, objectType);
        }
    }
}
