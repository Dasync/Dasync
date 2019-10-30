using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dasync.ValueContainer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dasync.Serialization.Json.Converters
{
    public class ValueContainerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IValueContainer).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var valueContainer = (IValueContainer)value;

            var serializedContainer = value as ISerializedValueContainer;
            if (serializedContainer != null && serializedContainer.GetFormat() == JsonSerializerAdapterFactory.FormatName)
            {
                var rawJson = (string)serializedContainer.GetSerializedForm();
                writer.WriteStartObject();
                writer.WriteRaw(rawJson.Substring(1, rawJson.Length - 2)); // TODO: set 'WriteState' to 'Object'?
                writer.WriteEndObject();
                //writer.GetType().GetMethod("SetWriteState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(writer, new object[] { JsonToken.StartObject, null });
                //writer.WriteRaw(rawJson);
                //writer.GetType().GetMethod("SetWriteState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(writer, new object[] { JsonToken.EndObject, null });
                return;
            }
            else
            {
                writer.WriteStartObject();
                for (var i = 0; i < valueContainer.GetCount(); i++)
                {
                    writer.WritePropertyName(valueContainer.GetName(i));
                    serializer.Serialize(writer, valueContainer.GetValue(i), valueContainer.GetType(i));
                }
                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var content = new StringBuilder();
                var textWriter = new StringWriter(content);
                var jsonWriter = new JsonTextWriter(textWriter);
                jsonWriter.WriteToken(reader, writeChildren: true);

                return new SerializedValueContainer(JsonSerializerAdapterFactory.FormatName, content.ToString(), null,
                    (format, form, state) =>
                    {
                        if (objectType == typeof(IValueContainer))
                        {
                            JObject jObj;
                            using (var stringReader = new StringReader((string)form))
                            {
                                jObj = (JObject)serializer.Deserialize(stringReader, typeof(JObject));
                            }
                            var container = ValueContainerFactory.Create(
                                ((IEnumerable<KeyValuePair<string, JToken>>)jObj)
                                .ToDictionary(p => p.Key, p => GetType(p.Value.Type)));
                            for (var i = 0; i < container.GetCount(); i++)
                            {
                                var value = typeof(Newtonsoft.Json.Linq.Extensions).GetMethods().First(m => m.Name == "Value" && m.GetGenericArguments().Length == 1).MakeGenericMethod(container.GetType(i)).Invoke(null, new object[] { jObj[container.GetName(i)] });
                                container.SetValue(i, value);
                            }
                            return container;
                        }
                        else
                        {

                        }
                        return null;
                    });
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot pasre the JSON envelope - unexpected JSON token '{reader.TokenType}'.");
            }
        }

        private static Type GetType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Null:
                    return typeof(object);
                case JTokenType.Object:
                    return typeof(IValueContainer);
                case JTokenType.Array:
                    return typeof(List<object>);
                case JTokenType.Integer:
                    return typeof(long);
                case JTokenType.Float:
                    return typeof(double);
                case JTokenType.String:
                    return typeof(string);
                case JTokenType.Boolean:
                    return typeof(bool);
                case JTokenType.Date:
                    return typeof(DateTimeOffset);
                case JTokenType.Bytes:
                    return typeof(byte[]);
                case JTokenType.Guid:
                    return typeof(Guid);
                case JTokenType.Uri:
                    return typeof(Uri);
                case JTokenType.TimeSpan:
                    return typeof(TimeSpan);
                default:
                    throw new Exception();
            }
        }
    }
}
