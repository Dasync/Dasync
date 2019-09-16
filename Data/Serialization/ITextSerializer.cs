using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ITextSerializer
    {
        string ContentType { get; }

        void Serialize(TextWriter writer, object @object);

        void Populate(TextReader reader, IValueContainer target);
    }
}
