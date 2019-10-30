using System.IO;
using Newtonsoft.Json;

namespace Dasync.Serialization.DasyncJson
{
    public class JsonValueWriterFactory : IValueWriterFactory, IValueTextWriterFactory
    {
        public IValueWriter Create(Stream stream)
        {
            var textWriter = new StreamWriter(stream, Encodings.UTF8, 4096, leaveOpen: true);
            var jsonWriter = new JsonTextWriter(textWriter);
            return new JsonValueWriter(jsonWriter);
        }

        public IValueWriter Create(TextWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer)
            {
                CloseOutput = false
            };
            return new JsonValueWriter(jsonWriter);
        }
    }
}
