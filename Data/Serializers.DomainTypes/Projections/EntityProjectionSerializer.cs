using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.DomainTypes.Projections
{
    public class EntityProjectionSerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly ITypeSerializerHelper _typeSerializerHelper;

        public EntityProjectionSerializer(ITypeSerializerHelper typeSerializerHelper)
        {
            _typeSerializerHelper = typeSerializerHelper;
        }

        public IValueContainer Decompose(object value)
        {
#warning Add support for multi-projection. Need to know the variable/result type - a bigger serializer problem :( Also possibly need to know if it's a cross-domain derialization to keep the entity instance.
            var projetionInterface = value.GetType().GetInterfaces().First(i => EntityProjection.IsProjectionInterface(i));

            var container = new EntityProjectionContainer
            {
                Type = _typeSerializerHelper.GetTypeSerializationInfo(projetionInterface).ToString(),
                Properties = new Dictionary<string, object>()
            };

#warning Add properties of base interface(s)
            foreach (var property in projetionInterface.GetProperties())
                container.Properties.Add(property.Name, property.GetValue(value));

            return container;
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var c = (EntityProjectionContainer)container;
            var projetionInterface = _typeSerializerHelper.ResolveType(TypeSerializationInfo.Parse(c.Type));
            var result = EntityProjection.CreateInstance(projetionInterface);
            if (c.Properties != null)
            {
                foreach (var pair in c.Properties)
                {
                    EntityProjection.SetValue(result, pair.Key, pair.Value);
                }
            }
            return result;
        }

        public IValueContainer CreatePropertySet(Type valueType) => new EntityProjectionContainer();
    }

    public class EntityProjectionContainer : ValueContainerBase, IValueContainerWithTypeInfo
    {
        public string Type;
        public Dictionary<string, object> Properties;

        public Type GetObjectType() => typeof(EntityProjectionBase);
    }
}
