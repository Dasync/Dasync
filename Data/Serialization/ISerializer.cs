using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ISerializer
    {
        string Format { get; }

        void Serialize(Stream stream, object @object);

        void Populate(Stream stream, IValueContainer target);
    }
}
