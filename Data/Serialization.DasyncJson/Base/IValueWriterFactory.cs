using System.IO;

namespace Dasync.Serialization
{
    public interface IValueWriterFactory
    {
        IValueWriter Create(Stream stream);
    }

    public interface IValueTextWriterFactory
    {
        IValueWriter Create(TextWriter writer);
    }
}
