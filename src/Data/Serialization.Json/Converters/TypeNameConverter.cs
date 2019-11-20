using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dasync.Serialization.Json.Converters
{
    public class TypeNameConverter : JsonConverter
    {
        private readonly ITypeNameShortener _typeNameShortener;

        public TypeNameConverter(IEnumerable<ITypeNameShortener> typeNameShorteners)
        {
            _typeNameShortener = new TypeNameShortenerChain(typeNameShorteners);
        }

        public override bool CanConvert(Type objectType) => typeof(Type).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (_typeNameShortener.TryShorten((Type)value, out var shortName))
                writer.WriteValue(shortName);
            else
                writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var typeName = reader.Value as string;

            if (!_typeNameShortener.TryExpand(typeName, out var type))
                type = Type.GetType(typeName);

            return type;
        }
    }
}
