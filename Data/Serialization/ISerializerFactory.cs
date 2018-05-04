namespace Dasync.Serialization
{
    public interface ISerializerFactory
    {
        string Format { get; }

        ISerializer Create();
    }
}
