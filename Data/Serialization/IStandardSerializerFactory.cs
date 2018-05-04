namespace Dasync.Serialization
{
    public interface IStandardSerializerFactory
    {
        ISerializer Create(
            IValueWriterFactory valueWriterFactory,
            IValueReaderFactory valueReaderFactory);
    }
}
