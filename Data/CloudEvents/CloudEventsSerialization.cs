using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dasync.CloudEvents
{
    public class CloudEventsSerialization
    {
        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        public static string Serialize(object @object)
        {
            return JsonConvert.SerializeObject(@object, JsonSerializerSettings);
        }
    }
}
