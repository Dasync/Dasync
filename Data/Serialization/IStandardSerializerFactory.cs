namespace Dasync.Serialization
{
    public interface IStandardSerializerFactory
    {
        ISerializer Create(
            string contentType,
            IValueWriterFactory valueWriterFactory,
            IValueReaderFactory valueReaderFactory);
    }
}
