namespace Dasync.Serialization.Json
{
    public class DasyncJsonSerializerFactory : ISerializerFactory
    {
        private readonly ISerializer _serializer;

        public DasyncJsonSerializerFactory(IStandardSerializerFactory standardSerializerFactory)
        {
            _serializer = standardSerializerFactory.Create(
                new JsonValueWriterFactory(),
                new JsonValueReaderFactory());
        }

        public string Format => "dasyncjson";

        public ISerializer Create()
        {
            return _serializer;
        }
    }
}
