using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ISerializer
    {
        void Serialize(Stream stream, object @object);

        void Populate(Stream stream, IValueContainer target);
    }
}
