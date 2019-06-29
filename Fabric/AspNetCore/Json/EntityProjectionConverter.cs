using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.Modeling;
using Dasync.Serializers.DomainTypes.Projections;
using Newtonsoft.Json;

namespace Dasync.AspNetCore.Json
{
    public class EntityProjectionConverter : JsonConverter
    {
        private readonly HashSet<Type> _knownProjectionInterfaces;

        public EntityProjectionConverter(ICommunicationModel model)
        {
            _knownProjectionInterfaces = new HashSet<Type>(model.EntityProjections.Select(p => p.InterfaceType));
        }

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            if (_knownProjectionInterfaces.Count == 0)
                return false;

            if (objectType.Assembly.IsDynamic)
                return false;

            if (objectType.IsInterface && _knownProjectionInterfaces.Contains(objectType))
                return true;

            if (!objectType.IsClass)
                return false;

            var interfaces = objectType.GetInterfaces();
            if (interfaces == null || interfaces.Length == 0)
                return false;

            for (var i = 0; i < interfaces.Length; i++)
                if (_knownProjectionInterfaces.Contains(interfaces[i]))
                    return true;

            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var projection = EntityProjection.CreateInstance(objectType);
            serializer.Populate(reader, projection);
            return projection;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var interfaces = value.GetType().GetInterfaces();
            Type projectionType = null;
            for (var i = 0; i < interfaces.Length; i++)
            {
                if (_knownProjectionInterfaces.Contains(interfaces[i]))
                {
                    projectionType = interfaces[i];
                    break;
                }
            }

            var projection = EntityProjection.CreateInstance(projectionType);
            foreach (var targetPropertyInfo in projectionType.GetProperties())
            {
                var sourcePropertyInfo = value.GetType().GetProperty(targetPropertyInfo.Name);
                var propertyValue = sourcePropertyInfo.GetValue(value);
                EntityProjection.SetValue(projection, targetPropertyInfo.Name, propertyValue);
            }
            serializer.Serialize(writer, projection);
        }
    }
}
