using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Dasync.Json
{
    public class NestedJsonConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType) => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stringValue = (string)value;
            if (stringValue == null)
            {
                writer.WriteNull();
            }
            else if (stringValue.Length == 0 || stringValue[0] != '{')
            {
                writer.WriteValue(stringValue);
            }
            else
            {
                writer.WriteRawValue(stringValue);
            }
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var content = new StringBuilder();
                var textWriter = new StringWriter(content);
                var jsonWriter = new JsonTextWriter(textWriter);
                jsonWriter.WriteToken(reader, writeChildren: true);
                return content.ToString();
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return (string)reader.Value;
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot pasre the JSON envelope - unexpected JSON token '{reader.TokenType}'.");
            }
        }
    }
}
