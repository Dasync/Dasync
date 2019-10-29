namespace Dasync.Serialization
{
    public interface IStandardSerializerFactory
    {
        ISerializer Create(
            string format,
            IValueWriterFactory valueWriterFactory,
            IValueReaderFactory valueReaderFactory);
    }
}
