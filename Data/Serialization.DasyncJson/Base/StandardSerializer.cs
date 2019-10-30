using System;
using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public class StandardSerializer : ISerializer
    {
        private readonly IValueWriterFactory _valueWriterFactory;
        private readonly IValueReaderFactory _valueReaderFactory;
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly IObjectComposerSelector _composerSelector;
        private readonly ITypeSerializerHelper _typeSerializerHelper;

        public StandardSerializer(
            string format,
            IValueWriterFactory valueWriterFactory,
            IValueReaderFactory valueReaderFactory,
            IObjectDecomposerSelector decomposerSelector,
            IObjectComposerSelector composerSelector,
            ITypeSerializerHelper typeSerializerHelper)
        {
            Format = format;
            _valueWriterFactory = valueWriterFactory;
            _valueReaderFactory = valueReaderFactory;
            _decomposerSelector = decomposerSelector;
            _composerSelector = composerSelector;
            _typeSerializerHelper = typeSerializerHelper;
        }

        public string Format { get; }

        public void Serialize(Stream stream, object @object, Type objectType)
        {
            if (@object == null)
                return;

            using (var valueWriter = _valueWriterFactory.Create(stream))
            {
                var serializer = new ValueContainerSerializer(_decomposerSelector, _typeSerializerHelper);
                serializer.Serialize(@object, valueWriter);
            }
        }

        public void Populate(Stream stream, IValueContainer target)
        {
            using (var valueReader = _valueReaderFactory.Create(stream))
            {
                var reconstructor = new ObjectReconstructor(_composerSelector, target, _typeSerializerHelper);
                valueReader.Read(reconstructor, this);
            }
        }

        public object Deserialize(Stream stream, Type objectType = null)
        {
            throw new NotImplementedException();
        }
    }
}
