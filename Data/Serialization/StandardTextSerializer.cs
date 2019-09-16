using System.IO;
using System.Text;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public class StandardTextSerializer : ISerializer, ITextSerializer
    {
        private static readonly Encoding UTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly IValueTextWriterFactory _valueTextWriterFactory;
        private readonly IValueTextReaderFactory _valueTextReaderFactory;
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly IObjectComposerSelector _composerSelector;
        private readonly ITypeSerializerHelper _typeSerializerHelper;

        public StandardTextSerializer(
            string contentType,
            IValueTextWriterFactory valueTextWriterFactory,
            IValueTextReaderFactory valueTextReaderFactory,
            IObjectDecomposerSelector decomposerSelector,
            IObjectComposerSelector composerSelector,
            ITypeSerializerHelper typeSerializerHelper)
        {
            ContentType = contentType;
            _valueTextWriterFactory = valueTextWriterFactory;
            _valueTextReaderFactory = valueTextReaderFactory;
            _decomposerSelector = decomposerSelector;
            _composerSelector = composerSelector;
            _typeSerializerHelper = typeSerializerHelper;
        }

        public string ContentType { get; }

        public void Serialize(TextWriter writer, object @object)
        {
            if (@object == null)
                return;

            using (var valueWriter = _valueTextWriterFactory.Create(writer))
            {
                var serializer = new ValueContainerSerializer(_decomposerSelector, _typeSerializerHelper);
                serializer.Serialize(@object, valueWriter);
            }
        }

        public void Populate(TextReader reader, IValueContainer target)
        {
            using (var valueReader = _valueTextReaderFactory.Create(reader))
            {
                var reconstructor = new ObjectReconstructor(_composerSelector, target, _typeSerializerHelper);
                valueReader.Read(reconstructor);
            }
        }

        public void Serialize(Stream stream, object @object)
        {
            if (@object == null)
                return;

            using (var textWriter = new StreamWriter(stream, UTF8, 4096, leaveOpen: true))
            {
                using (var valueWriter = _valueTextWriterFactory.Create(textWriter))
                {
                    var serializer = new ValueContainerSerializer(_decomposerSelector, _typeSerializerHelper);
                    serializer.Serialize(@object, valueWriter);
                }
            }
        }

        public void Populate(Stream stream, IValueContainer target)
        {
            using (var textReader = new StreamReader(stream, UTF8, true, 4096, leaveOpen: true))
            {
                using (var valueReader = _valueTextReaderFactory.Create(textReader))
                {
                    var reconstructor = new ObjectReconstructor(_composerSelector, target, _typeSerializerHelper);
                    valueReader.Read(reconstructor);
                }
            }
        }
    }
}
