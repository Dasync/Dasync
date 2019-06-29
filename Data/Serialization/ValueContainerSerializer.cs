using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if !NETSTANDARD1_4
using System.Runtime.Serialization;
#endif
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public class ValueContainerSerializer : IValueContainerSerializer
    {
        private readonly IObjectDecomposerSelector _decomposerSelector;
        private readonly ITypeSerializerHelper _typeSerializerHelper;
        private readonly ObjectIDGenerator _idGenerator = new ObjectIDGenerator();
        private readonly Dictionary<long, string> _specialIds = new Dictionary<long, string>();

        public ValueContainerSerializer(
            IObjectDecomposerSelector decomposerSelector,
            ITypeSerializerHelper typeSerializerHelper)
        {
            _decomposerSelector = decomposerSelector;
            _typeSerializerHelper = typeSerializerHelper;
        }

        public void Serialize(
            object value,
            IValueWriter writer,
            IEnumerable<KeyValuePair<string, object>> specialObjects = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (specialObjects != null)
            {
                foreach (var pair in specialObjects)
                {
                    var id = _idGenerator.GetId(pair.Value, out var _);
                    _specialIds.Add(id, pair.Key);
                }
            }

            ValueInfo valueInfo;
            if (value is IValueContainer container)
            {
#warning still need to write decomposition info for non-dynamic types
                valueInfo = new ValueInfo();
            }
            else
            {
                if (!TryDecomposeValue(value?.GetType(), value, out var typeInfo, out container))
                    throw new UnserializableTypeException(value?.GetType());
                valueInfo = new ValueInfo { Type = typeInfo };
            }

            writer.WriteStart();
            writer.WriteStartValue(valueInfo);
            SerializeValueContainer(container, writer);
            writer.WriteEndValue();
            writer.WriteEnd();
        }

        private void SerializeValueContainer(IValueContainer valueContainer, IValueWriter writer)
        {
            var valueCount = valueContainer.GetCount();
            for (var i = 0; i < valueCount; i++)
            {
                var name = valueContainer.GetName(i);
                var type = valueContainer.GetType(i);
                var value = valueContainer.GetValue(i);

                SerializeValue(name, type, value, writer);
            }
        }

        private void SerializeValue(string name, Type type, object value, IValueWriter writer,
            Type collectionItemType = null, int? index = null)
        {
            var valueInfo = new ValueInfo
            {
                Name = name,
                Index = index
            };

            if (value != null)
                type = value.GetType();

            var writeValueAsReference = false;
            if (value != null && !type.IsValueType() && type != typeof(string) && type != typeof(Uri))
            {
                var objectId = _idGenerator.GetId(value, out var isFirstTime);
                if (_specialIds.TryGetValue(objectId, out var specialId))
                {
                    valueInfo.SpecialId = specialId;
                    writeValueAsReference = true;
                }
                else
                {
                    valueInfo.ReferenceId = objectId;
                    writeValueAsReference = !isFirstTime;
                }
            }

            if (value is Type valueAsType)
            {
                valueInfo.Type = _typeSerializerHelper.GetTypeSerializationInfo(valueAsType);
                type = typeof(Type);
            }

            if (writeValueAsReference)
            {
                // Reset other fields, because they don't need to be written.
                valueInfo.Type = null;
                valueInfo.ItemType = null;
                valueInfo.ItemCount = null;

                writer.WriteStartValue(valueInfo);
                writer.WriteEndValue();
            }
            else if (value == null)
            {
                writer.WriteStartValue(valueInfo);
                writer.WriteValue(null);
                writer.WriteEndValue();
            }
            else if (writer.CanWriteValueWithoutTypeInfo(type))
            {
                writer.WriteStartValue(valueInfo);
                writer.WriteValue(value);
                writer.WriteEndValue();
            }
            else if (type.IsEnum())
            {
                writer.WriteStartValue(valueInfo);
                value = Convert.ChangeType(value, type.GetEnumUnderlyingType());
                writer.WriteValue(value);
                writer.WriteEndValue();
            }
#warning Treat other types of collections as arrays
            else if (type.IsArray)
            {
                var items = (IList)value;
                var itemType = type.GetElementType();

                valueInfo.IsCollection = true;
#warning Write collection type
                //valueInfo.Type = 
                valueInfo.ItemType = itemType == typeof(object) || itemType == collectionItemType ? null : _typeSerializerHelper.GetTypeSerializationInfo(itemType);
                valueInfo.ItemCount = items.Count;

                writer.WriteStartValue(valueInfo);
                var itemIndex = 0;
                foreach (var item in items)
                    SerializeValue(null, item?.GetType(), item, writer, collectionItemType: itemType, index: itemIndex++);
                writer.WriteEndValue();
            }
            else if (value is IValueContainer valueAsContainer)
            {
                writer.WriteStartValue(valueInfo);
                if (valueAsContainer.GetCount() == 0)
                {
                    writer.WriteValue(null);
                }
                else
                {
                    SerializeValueContainer(valueAsContainer, writer);
                }
                writer.WriteEndValue();
            }
            else
            {
                if (!TryDecomposeValue(type, value, out var typeInfo, out var nestedContainer))
                    throw new UnserializableTypeException(type);
                valueInfo.Type = typeInfo;
                writer.WriteStartValue(valueInfo);
                SerializeValueContainer(nestedContainer, writer);
                writer.WriteEndValue();
            }
        }

        private bool TryDecomposeValue(
            Type type, object value,
            out TypeSerializationInfo typeInfo,
            out IValueContainer container)
        {
            type = value?.GetType() ?? type;

            var decomposer = _decomposerSelector.SelectDecomposer(type);
            if (decomposer == null)
            {
                typeInfo = null;
                container = null;
                return false;
            }

            container = decomposer.Decompose(value);

            if (container is IValueContainerWithTypeInfo typeInfoProvider)
                type = typeInfoProvider.GetObjectType();

            typeInfo = _typeSerializerHelper.GetTypeSerializationInfo(type);
            return true;
        }
    }
}
