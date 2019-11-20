using System;
using System.IO;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ISerializer
    {
        string Format { get; }

        void Serialize(Stream stream, object @object, Type objectType = null);

        void Populate(Stream stream, IValueContainer target);

        object Deserialize(Stream stream, Type objectType = null);
    }
}
