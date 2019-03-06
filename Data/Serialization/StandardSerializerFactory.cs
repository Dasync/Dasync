namespace Dasync.Serialization
{
    public class StandardSerializerFactory : IStandardSerializerFactory
    {
        private readonly ITypeSerializerHelper _typeSerializerHelper;
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly IObjectComposerSelector _composerSelector;

        public StandardSerializerFactory(
            ITypeSerializerHelper typeSerializerHelper,
            IObjectDecomposerSelector decomposerSelector,
            IObjectComposerSelector composerSelector)
        {
            _typeSerializerHelper = typeSerializerHelper;
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
                    _decomposerSelector,
                    _composerSelector,
                    _typeSerializerHelper);

            return new StandardSerializer(
                valueWriterFactory,
                valueReaderFactory,
                _decomposerSelector,
                _composerSelector,
                _typeSerializerHelper);
        }
    }
}
