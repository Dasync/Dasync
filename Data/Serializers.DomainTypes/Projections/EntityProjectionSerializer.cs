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

        internal HashSet<Type> KnownEntityProjectionInterfaces { get; set; }

        public IValueContainer Decompose(object value)
        {
#warning Add support for multi-projection. Need to know the variable/result type - a bigger serializer problem :( Also possibly need to know if it's a cross-domain derialization to keep the entity instance.
            var projetionInterface = value.GetType().GetInterfaces()
                .First(i => KnownEntityProjectionInterfaces != null
                    ? KnownEntityProjectionInterfaces.Contains(i)
                    : EntityProjection.IsProjectionInterface(i));

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
                    // BAD CODE
                    // The problem is in JSON serialization, so we have to perform some extra type checks and conversion.
                    // I know, it's bad, just need to make it work as a quick-fix without going back to the drawing board.
                    var value = pair.Value;
                    if (value != null)
                    {
                        var expectedValueType = projetionInterface.GetProperty(pair.Key).PropertyType;
                        var actualValueType = value.GetType();
                        if (actualValueType != expectedValueType)
                        {
                            if ((expectedValueType == typeof(Guid) || expectedValueType == typeof(Guid?)) && actualValueType == typeof(string))
                            {
                                value = Guid.Parse((string)value);
                            }
                            else if (expectedValueType == typeof(Uri) && actualValueType == typeof(string))
                            {
                                value = new Uri((string)value);
                            }
                            else if (expectedValueType.IsEnum)
                            {
                                value = Enum.ToObject(expectedValueType, value);
                            }
                            else if (expectedValueType.IsGenericType && !expectedValueType.IsClass && expectedValueType.Name == "Nullable`1")
                            {
                                var nullableValueType = expectedValueType.GetGenericArguments()[0];
                                if (nullableValueType != actualValueType)
                                    value = Convert.ChangeType(value, nullableValueType);
                                value = Activator.CreateInstance(expectedValueType, value);
                            }
                            else
                            {
                                value = Convert.ChangeType(value, expectedValueType);
                            }
                        }
                    }
                    // BAD CODE

                    EntityProjection.SetValue(result, pair.Key, value);
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
