using System.IO;
using Newtonsoft.Json;

namespace Dasync.Serialization.DasyncJson
{
    public class JsonValueReaderFactory : IValueReaderFactory, IValueTextReaderFactory
    {
        public IValueReader Create(Stream stream)
        {
            var textReader = new StreamReader(stream, Encodings.UTF8, false, 4096, leaveOpen: true);
            var jsonReader = new JsonTextReader(textReader);
            return new JsonValueReader(jsonReader);
        }

        public IValueReader Create(TextReader reader)
        {
            var jsonReader = new JsonTextReader(reader)
            {
                CloseInput = false
            };
            return new JsonValueReader(jsonReader);
        }
    }
}
