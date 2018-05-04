namespace Dasync.Serialization
{
    public class StandardSerializerFactory : IStandardSerializerFactory
    {
        private readonly ITypeNameShortener _typeNameShortener;
        private readonly IAssemblyNameShortener _assemblyNameShortener;
        private readonly ITypeResolver _typeResolver;
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly IObjectComposerSelector _composerSelector;

        public StandardSerializerFactory(
            ITypeNameShortener typeNameShortener,
            IAssemblyNameShortener assemblyNameShortener,
            ITypeResolver typeResolver,
            IObjectDecomposerSelector decomposerSelector,
            IObjectComposerSelector composerSelector)
        {
            _typeNameShortener = typeNameShortener;
            _assemblyNameShortener = assemblyNameShortener;
            _typeResolver = typeResolver;
            _decomposerSelector = decomposerSelector;
            _composerSelector = composerSelector;
        }

        public ISerializer Create(
            IValueWriterFactory valueWriterFactory,
            IValueReaderFactory valueReaderFactory)
        {
            if (valueWriterFactory is IValueTextWriterFactory valueTextWriterFactory &&
                valueReaderFactory is IValueTextReaderFactory valueTextReaderFactory)
                return new StandardTextSerializer(
                    valueTextWriterFactory,
                    valueTextReaderFactory,
                    _typeNameShortener,
                    _assemblyNameShortener,
                    _typeResolver,
                    _decomposerSelector,
                    _composerSelector);

            return new StandardSerializer(
                valueWriterFactory,
                valueReaderFactory,
                _typeNameShortener,
                _assemblyNameShortener,
                _typeResolver,
                _decomposerSelector,
                _composerSelector);
        }
    }
}
