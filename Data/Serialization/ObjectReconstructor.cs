using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public class ObjectReconstructor : IObjectReconstructor
    {
        private sealed class ScopeState
        {
            public ScopeState ParentScope;
            public IValueContainer Container;
            public IObjectComposer Composer;
            public ValueInfo ValueInfo;
            public Type Type;
            public Type ItemType;
            public IList Array;
            public int Index = -1;
            public object Value;
            public bool ValueReceived;
            public bool IsDynamicContainer;
        }

        private Stack<ScopeState> _scopeStack = new Stack<ScopeState>();
        private ScopeState _scope;
        private readonly IObjectComposerSelector _composerSelector;
        private Dictionary<long, object> _objectByIdMap = new Dictionary<long, object>();
        private Dictionary<string, object> _objectByNameMap;
        private IValueContainer _targetRootContainer;
        private readonly ITypeSerializerHelper _typeSerializerHelper;

        public ObjectReconstructor(
            IObjectComposerSelector composerSelector,
#warning target should be optional (deserialize vs populate) and of type "object"
            IValueContainer target,
            ITypeSerializerHelper typeSerializerHelper,
            Dictionary<string, object> objectByNameMap = null)
        {
            _composerSelector = composerSelector;
            _objectByNameMap = objectByNameMap;
            _targetRootContainer = target;
            _typeSerializerHelper = typeSerializerHelper;
        }

        public void OnValueStart(ValueInfo info)
        {
            _scope = new ScopeState
            {
                ParentScope = _scope,
                Container = _scope == null ? _targetRootContainer : null
            };
            _scope.ValueInfo = info;

            if (info.Type != null)
            {
                _scope.Type = _typeSerializerHelper.ResolveType(info.Type);
                if (_scope.Container == null)
                {
                    _scope.Composer = _composerSelector.SelectComposer(_scope.Type);
                    if (_scope.Composer == null)
                        throw new UnserializableTypeException(_scope.Type);
                    _scope.Container = _scope.Composer.CreatePropertySet(_scope.Type);
                }
            }

            if (info.IsCollection)
            {
                _scope.ItemType = info.ItemType != null
                    ? _typeSerializerHelper.ResolveType(info.ItemType)
                    : typeof(object);

                _scope.Array = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_scope.ItemType));
                _scope.Value = _scope.Array;
                _scope.ValueReceived = true;
            }
        }

        public void OnValue(object value)
        {
            _scope.Value = value;
            _scope.ValueReceived = true;
        }

        public void OnValueEnd()
        {
            if (_scope.ParentScope != null
                && _scope.ParentScope.Composer == null
                && _scope.ParentScope.Container == null)
            {
                _scope.ParentScope.IsDynamicContainer = true;
            }

            if (!_scope.ValueReceived && _scope.Composer != null)
            {
                _scope.Value = _scope.Composer.Compose(_scope.Container, _scope.Type);
                _scope.ValueReceived = true;
            }

            if (!_scope.ValueReceived && _scope.IsDynamicContainer)
            {
                _scope.Value = _scope.Container;
                _scope.ValueReceived = true;
            }

            if (!_scope.ValueReceived && !_scope.ValueInfo.IsCollection)
            {
                if (_scope.ValueInfo.ReferenceId.HasValue)
                {
                    if (!_objectByIdMap.TryGetValue(_scope.ValueInfo.ReferenceId.Value, out _scope.Value))
                        throw new InvalidOperationException($"Could not find object by ID '{_scope.ValueInfo.ReferenceId.Value}'.");
                    _scope.ValueReceived = true;
                }
                else if (_scope.ValueInfo.SpecialId != null)
                {
                    if (_objectByNameMap == null || !_objectByNameMap.TryGetValue(_scope.ValueInfo.SpecialId, out _scope.Value))
                        throw new InvalidOperationException($"Could not find object by ID '{_scope.ValueInfo.SpecialId}'.");
                    _scope.ValueReceived = true;
                }
            }

            var valueScope = _scope;
            _scope = _scope.ParentScope;

            if (valueScope.ValueReceived)
            {
                var value = valueScope.Value;

#warning Support various collection types. Currently supports arrays only.
                if (valueScope.ValueInfo.IsCollection)
                {
                    value = this.GetType()
                        .GetMethod(nameof(ToArray), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(value.GetType().GetGenericArguments()[0])
                        .Invoke(null, new object[] { value });
                }

                Type targetType;
                if (_scope.ValueInfo.IsCollection)
                {
                    targetType = _scope.ItemType;
                }
                else if (_scope.IsDynamicContainer)
                {
                    targetType = value?.GetType();
                }
                else
                {
                    var startIndex = _scope.Index + 1;

                    _scope.Index = FindIndex(_scope.Container, valueScope.ValueInfo.Name, startIndex);

                    // Try to find by value type if the argument was renamed.
                    if (_scope.Index < 0 && value != null)
                        _scope.Index = FindIndex(_scope.Container, value.GetType());

                    if (_scope.Index >= 0)
                    {
                        targetType = _scope.Container.GetType(_scope.Index);
                    }
                    else
                    {
                        // TODO: better skip logic?
                        value = null;
                        targetType = null;
                        _scope.Index = startIndex - 1;
                    }
                }

                if (value != null && targetType != null)
                {
                    // TODO: converter
                    if (!targetType.IsAssignableFrom(value.GetType()))
                    {
                        if (targetType.IsArray || ((value is IList) && targetType == typeof(object)))
                        {
#warning This byte[] conversion is a quick-fix. Do it properly by adding type during serialization?
                            if (targetType == typeof(byte[]) && value is string)
                                value = Convert.FromBase64String((string)value);
                            else
                                value = this.GetType()
                                    .GetMethod(nameof(ToArray), BindingFlags.Static | BindingFlags.NonPublic)
                                    .MakeGenericMethod(targetType.GetElementType() ?? value.GetType().GetGenericArguments()[0])
                                    .Invoke(null, new object[] { value });
                        }
                        else if (targetType.IsEnum())
                        {
                            value = Enum.ToObject(targetType, value);
                        }
                        else if ((targetType == typeof(Guid) || targetType == typeof(Guid?)) && value is string strGuid)
                        {
                            value = Guid.Parse(strGuid);
                        }
                        else if (targetType is Type && value is TypeSerializationInfo typeInfo)
                        {
                            value = _typeSerializerHelper.ResolveType(typeInfo);
                        }
                        else if (targetType == typeof(Uri))
                        {
                            value = new Uri((string)Convert.ChangeType(value, typeof(string)));
                        }
                        else if (targetType.IsGenericType && !targetType.IsClass && targetType.Name == "Nullable`1")
                        {
                            var nullableValueType = targetType.GetGenericArguments()[0];
                            if (!nullableValueType.IsAssignableFrom(value.GetType()))
                                value = Convert.ChangeType(value, nullableValueType);
                            value = Activator.CreateInstance(targetType, value);
                        }
                        else
                        {
                            value = Convert.ChangeType(value, targetType);
                        }
                    }

                    if (valueScope.ValueInfo.ReferenceId.HasValue)
                    {
                        _objectByIdMap[valueScope.ValueInfo.ReferenceId.Value] = value;
                    }

                    if (_scope.ValueInfo.IsCollection)
                    {
                        _scope.Array.Add(value);
                    }
                    else if (_scope.IsDynamicContainer)
                    {
                        var dynamicContainer = _scope.Container as ValueContainer.ValueContainer;
                        if (dynamicContainer == null)
                        {
                            dynamicContainer = new ValueContainer.ValueContainer();
                            _scope.Container = dynamicContainer;
                        }
                        dynamicContainer.Add(valueScope.ValueInfo.Name, targetType, value);
                    }
                    else
                    {
                        _scope.Container.SetValue(_scope.Index, value);
                    }
                }
            }
        }

        static T[] ToArray<T>(IList list) => list.Cast<T>().ToArray();

        private static int FindIndex(IValueContainer container, string nameToFind, int startIndex)
        {
            var count = container.GetCount();
            if (startIndex < 0 || startIndex >= count)
            {
                startIndex = 0;
            }

            for (var i = startIndex; i < count; i++)
            {
                var name = container.GetName(i);
                if (string.Equals(name, nameToFind, StringComparison.Ordinal))
                    return i;
            }

            for (var i = 0; i < startIndex; i++)
            {
                var name = container.GetName(i);
                if (string.Equals(name, nameToFind, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static int FindIndex(IValueContainer container, Type uniqueTypeToFind)
        {
            var foundIndex = -1;

            var count = container.GetCount();
            for (var i = 0; i < count; i++)
            {
                var type = container.GetType(i);
                if (ReferenceEquals(type, uniqueTypeToFind) || (type.IsInterface && type.IsAssignableFrom(uniqueTypeToFind)))
                {
                    if (foundIndex >= 0)
                        return -1;

                    foundIndex = i;
                }
            }

            return foundIndex;
        }
    }
}
