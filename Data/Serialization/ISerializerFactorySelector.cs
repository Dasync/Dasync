namespace Dasync.Serialization
{
    public interface ISerializerFactorySelector
    {
        ISerializerFactory Select(string format);
    }
}
