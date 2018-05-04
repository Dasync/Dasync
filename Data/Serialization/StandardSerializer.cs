using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public class StandardSerializer : ISerializer
    {
        private readonly IValueWriterFactory _valueWriterFactory;
        private readonly IValueReaderFactory _valueReaderFactory;
        private readonly ITypeNameShortener _typeNameShortener;
        private readonly IAssemblyNameShortener _assemblyNameShortener;
        private readonly ITypeResolver _typeResolver;
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly IObjectComposerSelector _composerSelector;

        public StandardSerializer(
            IValueWriterFactory valueWriterFactory,
            IValueReaderFactory valueReaderFactory,
            ITypeNameShortener typeNameShortener,
            IAssemblyNameShortener assemblyNameShortener,
            ITypeResolver typeResolver,
            IObjectDecomposerSelector decomposerSelector,
            IObjectComposerSelector composerSelector)
        {
            _valueWriterFactory = valueWriterFactory;
            _valueReaderFactory = valueReaderFactory;
            _typeNameShortener = typeNameShortener;
            _assemblyNameShortener = assemblyNameShortener;
            _typeResolver = typeResolver;
            _decomposerSelector = decomposerSelector;
            _composerSelector = composerSelector;
        }

        public void Serialize(Stream stream, object @object)
        {
            if (@object == null)
                return;

            using (var valueWriter = _valueWriterFactory.Create(stream))
            {
                var serializer = new ValueContainerSerializer(
                    _decomposerSelector,
                    _typeNameShortener,
                    _assemblyNameShortener);

                serializer.Serialize(@object, valueWriter);
            }
        }

        public void Populate(Stream stream, IValueContainer target)
        {
            using (var valueReader = _valueReaderFactory.Create(stream))
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
