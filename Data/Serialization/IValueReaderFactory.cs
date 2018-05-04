using System.IO;

namespace Dasync.Serialization
{
    public interface IValueReaderFactory
    {
        IValueReader Create(Stream stream);
    }

    public interface IValueTextReaderFactory
    {
        IValueReader Create(TextReader reader);
    }
}
