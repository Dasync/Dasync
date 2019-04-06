using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Dasync.Serialization.Json
{
    public class JsonValueWriter : IValueWriter
    {
        private static readonly JsonSerializer _jsonSerializer =
            new JsonSerializer
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

        private static readonly TypeSerializationInfo TypeSerializationInfoTypeSerializationInfo =
            typeof(TypeSerializationInfo).ToTypeSerializationInfo();

        private readonly JsonWriter _jsonWriter;
        private readonly Stack<ValueWriteState> _valueInfoStack = new Stack<ValueWriteState>();

        private sealed class ValueWriteState
        {
            public ValueInfo Info;
            public bool MetadataWritten;
            public bool ValueStartWritten;
        }

        public JsonValueWriter(JsonWriter jsonWriter)
        {
            _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
        }

        public bool CanWriteValueWithoutTypeInfo(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(bool?) ||
                type == typeof(sbyte) ||
                type == typeof(sbyte?) ||
                type == typeof(byte) ||
                type == typeof(byte?) ||
                type == typeof(byte[]) ||
                type == typeof(short) ||
                type == typeof(short?) ||
                type == typeof(ushort) ||
                type == typeof(ushort?) ||
                type == typeof(int) ||
                type == typeof(int?) ||
                type == typeof(uint) ||
                type == typeof(uint?) ||
                type == typeof(long) ||
                type == typeof(long?) ||
                type == typeof(ulong) ||
                type == typeof(ulong?) ||
                type == typeof(float) ||
                type == typeof(float?) ||
                type == typeof(double) ||
                type == typeof(double?) ||
                type == typeof(decimal) ||
                type == typeof(decimal?) ||
                type == typeof(string) ||
                type == typeof(Uri) ||
                type == typeof(Guid) ||
                type == typeof(Guid?) ||
                type == typeof(Type) ||
                type == typeof(TypeSerializationInfo);
#warning Check if the type has JSON attributes on it or its properties/fields
        }

        public void WriteStart()
        {
        }

        public void WriteEnd()
        {
        }

        private void EnsureStartWritten(bool writingValue)
        {
            if (_valueInfoStack.Count > 0)
            {
                var state = _valueInfoStack.Peek();
                if (!state.ValueStartWritten)
                {
                    if (state.Info.IsCollection)
                    {
                        if (state.MetadataWritten)
                            _jsonWriter.WritePropertyName("$items");
                        _jsonWriter.WriteStartArray();
                    }
                    else if (writingValue)
                    {
                        if (state.MetadataWritten)
                            _jsonWriter.WritePropertyName("$value");
                    }
                    else if (state.Info.Type == null && !state.MetadataWritten)
                    {
                        // Serializaing a IValueContainer without type information.
                        // Thus metadata has never been written, hence the object start.
                        _jsonWriter.WriteStartObject();
                        state.MetadataWritten = true;
                    }
                    state.ValueStartWritten = true;
                }
            }
        }

        public void WriteStartValue(ValueInfo info)
        {
            var isInitialWrite = _valueInfoStack.Count == 0;

            EnsureStartWritten(false);

            var state = new ValueWriteState
            {
                Info = info
            };
            _valueInfoStack.Push(state);

            if (info.Name != null)
                _jsonWriter.WritePropertyName(info.Name);

            if (isInitialWrite || info.Type != null || info.ReferenceId != null || info.SpecialId != null)
            {
                _jsonWriter.WriteStartObject();
                state.MetadataWritten = true;
            }

            if (info.ReferenceId != null)
            {
                _jsonWriter.WritePropertyName("$id");
                _jsonWriter.WriteValue(info.ReferenceId.Value);
            }
            else if (info.SpecialId != null)
            {
                _jsonWriter.WritePropertyName("$id");
                _jsonWriter.WriteValue(info.SpecialId);
            }

            if (info.Type != null)
                WriteTypeInfo(info.Type);

            if (info.ItemType != null)
                WriteTypeInfo(info.ItemType, propName: "$itemType");

            if (info.ItemCount != null)
            {
                _jsonWriter.WritePropertyName("$itemCount");
                _jsonWriter.WriteValue(info.ItemCount.Value);
            }
        }

        public void WriteValue(object value)
        {
            var valueType = value?.GetType();

            if (valueType != null && valueType == typeof(TypeSerializationInfo))
            {
                EnsureStartWritten(false);
                WriteTypeInfo((TypeSerializationInfo)value);
                _jsonWriter.WritePropertyName("$typeIsValue");
                _jsonWriter.WriteValue(true);
            }
            else if (valueType != null && typeof(Type)
#if NETSTANDARD
                .GetTypeInfo()
#endif
                .IsAssignableFrom(valueType
#if NETSTANDARD
                .GetTypeInfo()
#endif
                ))
            {
                var valueInfo = _valueInfoStack.Peek();
                // If 'Info.Type' is not null, the type info has already been written in WriteStartValue.
                // That's important to support short type names.
                if (valueInfo.Info.Type == null)
                {
                    EnsureStartWritten(false);
                    WriteTypeInfo(valueType.ToTypeSerializationInfo());
                }
                _jsonWriter.WritePropertyName("$typeIsValue");
                _jsonWriter.WriteValue(true);
            }
            else
            {
                EnsureStartWritten(true);
                _jsonWriter.WriteValue(value);
            }
        }

        public void WriteEndValue()
        {
            var state = _valueInfoStack.Pop();
            if (state.ValueStartWritten && state.Info.IsCollection)
                _jsonWriter.WriteEndArray();
            if (state.MetadataWritten)
                _jsonWriter.WriteEndObject();
        }

        private void WriteTypeInfo(TypeSerializationInfo typeInfo, string propName = "$type")
        {
            if (typeInfo == null)
                return;

            if (propName != null)
                _jsonWriter.WritePropertyName(propName);

            if (typeInfo == TypeSerializationInfo.Self)
            {
                _jsonWriter.WriteValue("$type");
            }
            else if (typeInfo.Assembly == null && typeInfo.GenericArgs == null)
            {
                _jsonWriter.WriteValue(typeInfo.Name);
            }
            else
            {
                _jsonWriter.WriteStartObject();

                _jsonWriter.WritePropertyName(nameof(TypeSerializationInfo.Name));
                _jsonWriter.WriteValue(typeInfo.Name);

                if (typeInfo.GenericArgs != null)
                {
                    _jsonWriter.WritePropertyName(nameof(TypeSerializationInfo.GenericArgs));
                    _jsonWriter.WriteStartArray();
                    foreach (var genericArgType in typeInfo.GenericArgs)
                        WriteTypeInfo(genericArgType, propName: null);
                    _jsonWriter.WriteEndArray();
                }

                if (typeInfo.Assembly != null)
                {
                    _jsonWriter.WritePropertyName(nameof(TypeSerializationInfo.Assembly));

                    if (typeInfo.Assembly.Token == null && typeInfo.Assembly.Version == null)
                    {
                        _jsonWriter.WriteValue(typeInfo.Assembly.Name);
                    }
                    else
                    {
                        _jsonSerializer.Serialize(_jsonWriter, typeInfo.Assembly);
                    }
                }

                _jsonWriter.WriteEndObject();
            }
        }

        public void Dispose()
        {
            _jsonWriter.Close();
        }
    }
}
