using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dasync.ValueContainer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dasync.Serialization.DasyncJson
{
    public class JsonValueReader : IValueReader
    {
        private readonly JsonReader _jsonReader;

        public JsonValueReader(JsonReader jsonReader)
        {
            _jsonReader = jsonReader;
        }

        private sealed class State
        {
            public ValueInfo Info;
            public bool HasMetadata;
            public bool ValueStarted;
            public bool ValueEnded;
        }

        public void Read(IObjectReconstructor reconstructor, ISerializer serializer)
        {
            var stateStack = new Stack<State>();
            State state = null;

            while (_jsonReader.Read())
            {
                switch (_jsonReader.TokenType)
                {
                    case JsonToken.StartObject:
                        {
                            if (state?.Info.Name != null && reconstructor.GetExpectedValueType(state.Info.Name) == typeof(IValueContainer))
                            {
                                var content = new StringBuilder();
                                var textWriter = new StringWriter(content);
                                var jsonWriter = new JsonTextWriter(textWriter);
                                jsonWriter.WriteToken(_jsonReader, writeChildren: true);

                                var serializedContainer = new SerializedValueContainer(
                                    "dasync+json",
                                    content.ToString(),
                                    null,
                                    (format, form, state) =>
                                    {
                                        var dynamicValueContainer = new ValueContainer.ValueContainer();
                                        serializer.Populate((string)form, dynamicValueContainer);
                                        return dynamicValueContainer;
                                    });

                                reconstructor.OnValueStart(state.Info);
                                reconstructor.OnValue(serializedContainer);
                                reconstructor.OnValueEnd();

                                if (stateStack.Count > 0)
                                {
                                    state = stateStack.Pop();

                                    if (state.Info.IsCollection)
                                    {
                                        stateStack.Push(state);
                                        state = new State();
                                    }
                                }
                                else
                                {
                                    state = null;
                                }
                            }
                            else
                            {
                                if (state == null)
                                    state = new State();
                                state.HasMetadata = true;
                            }
                        }
                        break;

                    case JsonToken.EndObject:
                        {
                            if (state.ValueStarted && !state.ValueEnded)
                            {
                                reconstructor.OnValueEnd();
                                state.ValueEnded = true;
                            }
                            else if (!state.ValueStarted && (state.Info.ReferenceId != null || state.Info.SpecialId != null))
                            {
                                reconstructor.OnValueStart(state.Info);
                                reconstructor.OnValueEnd();
                            }

                            if (stateStack.Count > 0)
                            {
                                state = stateStack.Pop();

                                if (state.Info.IsCollection)
                                {
                                    stateStack.Push(state);
                                    state = new State();
                                }
                            }
                            else
                            {
                                state = null;
                            }
                        }
                        break;

                    case JsonToken.PropertyName:
                        {
                            var name = (string)_jsonReader.Value;
                            if (name[0] == '$')
                            {
                                if (name == "$id")
                                {
                                    ReadObjectId(ref state.Info);
                                }
                                else if (name == "$itemCount")
                                {
                                    ReadItemCount(ref state.Info);
                                }
                                else if (name == "$type")
                                {
                                    state.Info.Type = ReadTypeInfo();
                                }
                                else if (name == "$itemType")
                                {
                                    state.Info.IsCollection = true;

                                    state.Info.ItemType = ReadTypeInfo();
                                }
                                else if (name == "$items")
                                {
                                    state.Info.IsCollection = true;

                                    if (!state.ValueStarted)
                                    {
                                        reconstructor.OnValueStart(state.Info);
                                        state.ValueStarted = true;
                                    }
                                }
                                else if (name == "$value")
                                {
                                    if (!state.ValueStarted)
                                    {
                                        reconstructor.OnValueStart(state.Info);
                                        state.ValueStarted = true;
                                    }
                                }
                                else if (name == "$typeIsValue")
                                {
                                    if (ReadTypeIsValue())
                                    {
                                        var value = state.Info.Type;
                                        if (!state.ValueStarted)
                                        {
                                            state.Info.Type = TypeSerializationInfo.Self;
                                            reconstructor.OnValueStart(state.Info);
                                            state.ValueStarted = true;
                                        }
                                        reconstructor.OnValue(value);
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException($"Unknown directive '{name}'");
                                }
                            }
                            else
                            {
                                if (!state.ValueStarted)
                                {
                                    reconstructor.OnValueStart(state.Info);
                                    state.ValueStarted = true;
                                }
                                stateStack.Push(state);
                                state = new State
                                {
                                    Info = new ValueInfo
                                    {
                                        Name = name
                                    }
                                };
                            }
                        }
                        break;

                    case JsonToken.StartArray:
                        {
                            state.Info.IsCollection = true;

                            if (!state.ValueStarted)
                            {
                                reconstructor.OnValueStart(state.Info);
                                state.ValueStarted = true;
                            }
                            stateStack.Push(state);

                            state = new State();
                        }
                        break;

                    case JsonToken.EndArray:
                        {
                            state = stateStack.Pop();
                            if (!state.HasMetadata && state.ValueStarted)
                            {
                                reconstructor.OnValueEnd();
                                state.ValueEnded = true;
                            }
                        }
                        break;

                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                    case JsonToken.Date:
                    case JsonToken.Float:
                    case JsonToken.Integer:
                    case JsonToken.Null:
                    case JsonToken.String:
                        {
                            if (!state.HasMetadata)
                                reconstructor.OnValueStart(state.Info);

                            reconstructor.OnValue(_jsonReader.Value);

                            if (!state.HasMetadata)
                            {
                                reconstructor.OnValueEnd();

                                if (state.Info.Name != null)
                                    state = stateStack.Pop();
                            }
                        }
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unexpected JSON token '{_jsonReader.TokenType}'.");
                }
            }
        }

        private void ReadObjectId(ref ValueInfo valueInfo)
        {
            if (!_jsonReader.Read())
                throw new InvalidOperationException("Unexpected end of the JSON stream.");

            if (_jsonReader.TokenType == JsonToken.Integer)
            {
                valueInfo.ReferenceId = (long)_jsonReader.Value;
            }
            else if (_jsonReader.TokenType == JsonToken.String)
            {
                valueInfo.SpecialId = (string)_jsonReader.Value;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unexpected JSON token '{_jsonReader.TokenType}' when reading '$id'.");
            }
        }

        private void ReadItemCount(ref ValueInfo valueInfo)
        {
            if (!_jsonReader.Read())
                throw new InvalidOperationException("Unexpected end of the JSON stream.");

            if (_jsonReader.TokenType == JsonToken.Integer)
            {
                valueInfo.ItemCount = (int)(long)_jsonReader.Value;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unexpected JSON token '{_jsonReader.TokenType}' when reading '$itemCount'.");
            }
        }

        private TypeSerializationInfo ReadTypeInfo()
        {
            if (!_jsonReader.Read())
                throw new InvalidOperationException("Unexpected end of the JSON stream.");

#warning TODO: slow - optimize
            var typeObject = JToken.ReadFrom(_jsonReader);
            return ConvertToTypeSerializationInfo(typeObject);
        }

        private TypeSerializationInfo ConvertToTypeSerializationInfo(JToken typeObject)
        {
            if (typeObject.Type == JTokenType.String)
            {
                var typeName = (string)typeObject;

                if (typeName == "$type")
                    return TypeSerializationInfo.Self;

                return new TypeSerializationInfo
                {
                    Name = typeName
                };
            }
            else if (typeObject.Type == JTokenType.Object)
            {
                var result = new TypeSerializationInfo
                {
                    Name = typeObject.Value<string>(nameof(TypeSerializationInfo.Name))
                };

                var assemblyValue = typeObject[nameof(TypeSerializationInfo.Assembly)];
                if (assemblyValue != null)
                {
                    if (assemblyValue.Type == JTokenType.String)
                    {
                        result.Assembly = new AssemblySerializationInfo
                        {
                            Name = assemblyValue.Value<string>()
                        };
                    }
                    else if (assemblyValue.Type == JTokenType.Object)
                    {
                        result.Assembly = assemblyValue.ToObject<AssemblySerializationInfo>();
                    }
                    else
                    {
                        throw new Exception($"Invalid assembly token: '{assemblyValue.Type}'");
                    }
                }

                var genericArgsValues = typeObject[nameof(TypeSerializationInfo.GenericArgs)];
                if (genericArgsValues != null)
                {
                    if (genericArgsValues.Type == JTokenType.Array)
                    {
                        var genericArguments = new List<TypeSerializationInfo>();
                        foreach (var genericArgTypeValue in genericArgsValues)
                        {
                            var genericArgument = ConvertToTypeSerializationInfo(genericArgTypeValue);
                            genericArguments.Add(genericArgument);
                        }
                        result.GenericArgs = genericArguments.ToArray();
                    }
                    else
                    {
                        throw new Exception($"Invalid generic arguments token: '{genericArgsValues.Type}'");
                    }
                }

                return result;
            }
            else
            {
                throw new Exception($"Invalid type info token: '{typeObject.Type}'");
            }
        }

        private bool ReadTypeIsValue()
        {
            if (!_jsonReader.Read())
                throw new InvalidOperationException("Unexpected end of the JSON stream.");

            if (_jsonReader.TokenType == JsonToken.Boolean)
            {
                return (bool)_jsonReader.Value;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unexpected JSON token '{_jsonReader.TokenType}' when reading '$typeIsValue'.");
            }
        }

        public void Dispose()
        {
            _jsonReader.Close();
        }
    }
}
