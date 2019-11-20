using System;
using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ITextSerializer
    {
        string Format { get; }

        void Serialize(TextWriter writer, object @object, Type objectType = null);

        void Populate(TextReader reader, IValueContainer target);

        object Deserialize(TextReader reader, Type objectType = null);
    }
}
