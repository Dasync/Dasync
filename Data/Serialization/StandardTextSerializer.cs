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
        private readonly ITypeNameShortener _typeNameShortener;
        private readonly IAssemblyNameShortener _assemblyNameShortener;
        private readonly ITypeResolver _typeResolver;
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly IObjectComposerSelector _composerSelector;

        public StandardTextSerializer(
            IValueTextWriterFactory valueTextWriterFactory,
            IValueTextReaderFactory valueTextReaderFactory,
            ITypeNameShortener typeNameShortener,
            IAssemblyNameShortener assemblyNameShortener,
            ITypeResolver typeResolver,
            IObjectDecomposerSelector decomposerSelector,
            IObjectComposerSelector composerSelector)
        {
            _valueTextWriterFactory = valueTextWriterFactory;
            _valueTextReaderFactory = valueTextReaderFactory;
            _typeNameShortener = typeNameShortener;
            _assemblyNameShortener = assemblyNameShortener;
            _typeResolver = typeResolver;
            _decomposerSelector = decomposerSelector;
            _composerSelector = composerSelector;
        }

        public void Serialize(TextWriter writer, object @object)
        {
            if (@object == null)
                return;

            using (var valueWriter = _valueTextWriterFactory.Create(writer))
            {
                var serializer = new ValueContainerSerializer(
                    _decomposerSelector,
                    _typeNameShortener,
                    _assemblyNameShortener);

                serializer.Serialize(@object, valueWriter);
            }
        }

        public void Populate(TextReader reader, IValueContainer target)
        {
            using (var valueReader = _valueTextReaderFactory.Create(reader))
            {
                var reconstructor = new ObjectReconstructor(
                    _typeResolver,
                    _composerSelector,
                    target,
                    _typeNameShortener,
                    _assemblyNameShortener);

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
                    var serializer = new ValueContainerSerializer(
                        _decomposerSelector,
                        _typeNameShortener,
                        _assemblyNameShortener);

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
                    var reconstructor = new ObjectReconstructor(
                        _typeResolver,
                        _composerSelector,
                        target,
                        _typeNameShortener,
                        _assemblyNameShortener);

                    valueReader.Read(reconstructor);
                }
            }
        }
    }
}
