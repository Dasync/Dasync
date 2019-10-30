using Dasync.Serialization.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dasync.Serialization.Json
{
    public class JsonSerializerAdapterFactory : ISerializerFactory
    {
        public const string FormatName = "json";

        private JsonSerializerSettings _jsonSettings;

        public JsonSerializerAdapterFactory(TypeNameConverter typeNameConverter)
        {
            _jsonSettings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        OverrideSpecifiedNames = true,
                        ProcessDictionaryKeys = true,
                        //ProcessExtensionDataNames = true
                    }
                }
            };

            _jsonSettings.Converters.Add(typeNameConverter);
            _jsonSettings.Converters.Add(new ValueContainerConverter());
        }

        public string Format => FormatName;

        public ISerializer Create()
        {
            return new JsonSerializerAdapter(JsonSerializer.Create(_jsonSettings));
        }
    }
}
