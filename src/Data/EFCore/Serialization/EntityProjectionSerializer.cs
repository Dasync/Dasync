using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.Serialization;
using Dasync.Projections;
using Dasync.ValueContainer;

namespace Dasync.EntityFrameworkCore.Serialization
{
    public class EntityProjectionSerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly IKnownDbContextSet _knownDbContextSet;
        private readonly object _initializeLock = new object();
        private bool _isInitialized;
        private readonly Dictionary<Type, IValueContainerProxyFactory> _projectionTypes = new Dictionary<Type, IValueContainerProxyFactory>();

        public EntityProjectionSerializer(IKnownDbContextSet knownDbContextSet)
        {
            _knownDbContextSet = knownDbContextSet;
        }

        public bool CanSerialize(Type type)
        {
            SafeInitialize();
            return _projectionTypes.ContainsKey(type);
        }

        public bool CanDeserialize(Type type)
        {
            SafeInitialize();
            return _projectionTypes.ContainsKey(type);
        }

        public IValueContainer Decompose(object value)
        {
            var interfaceType = value.GetType().GetInterfaces()[0];
            var valuesProxyFactory = _projectionTypes[value.GetType()];
            var valuesProxy = valuesProxyFactory.Create(value);
            return new EntityProjectionContainer(interfaceType, valuesProxy);
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            var interfaceType = valueType;
            var projection = Projection.CreateInstance(interfaceType);
            var valuesProxyFactory = _projectionTypes[projection.GetType()];
            var valuesProxy = valuesProxyFactory.Create(projection);
            return new EntityProjectionContainer(interfaceType, valuesProxy, projection);
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            return ((EntityProjectionContainer)container).GetProjection();
        }

        private void SafeInitialize()
        {
            if (_isInitialized)
                return;

            lock (_initializeLock)
            {
                if (_isInitialized)
                    return;

                Initialize();

                _isInitialized = true;
            }
        }

        private void Initialize()
        {
            foreach (var pair in _knownDbContextSet.TypesAndModels)
            {
                var dbContextType = pair.Key;
                var model = pair.Value;

                foreach (var entityType in model.GetEntityTypes())
                {
                    if (entityType.ClrType == null)
                        continue;

                    foreach (var interfaceType in entityType.ClrType.GetInterfaces())
                    {
                        if (Projection.IsProjectionInterface(interfaceType))
                        {
                            var propertyNames = new HashSet<string>(interfaceType.GetProperties().Select(p => p.Name));
                            var valuesProxyFactoryForClrType = ValueContainerFactory.GetProxyFactory(
                                entityType.ClrType,
                                entityType.ClrType.GetProperties()
                                .Where(p => propertyNames.Contains(p.Name))
                                .Select(p => new KeyValuePair<string, MemberInfo>(p.Name, p)));
                            _projectionTypes.Add(interfaceType, valuesProxyFactoryForClrType);

                            var projectionType = Projection.GetProjectionType(interfaceType);
                            var valuesProxyFactoryForProjectionType = ValueContainerFactory.GetProxyFactory(
                                projectionType,
                                projectionType.GetProperties()
                                .Select(p => new KeyValuePair<string, MemberInfo>(p.Name, p)));
                            _projectionTypes.Add(projectionType, valuesProxyFactoryForProjectionType);
                        }
                    }
                }
            }
        }
    }

    public sealed class EntityProjectionContainer : IValueContainer, IValueContainerWithTypeInfo
    {
        private Type _interfaceType;
        private IValueContainer _valuesProxy;
        private object _projection;

        public EntityProjectionContainer(Type interfaceType, IValueContainer valuesProxy, object projection = null)
        {
            _interfaceType = interfaceType;
            _valuesProxy = valuesProxy;
            _projection = projection;
        }

        public object GetProjection() => _projection;

        public int GetCount() => _valuesProxy.GetCount();

        public string GetName(int index) => _valuesProxy.GetName(index);

        public Type GetType(int index) => _valuesProxy.GetType(index);

        public object GetValue(int index) => _valuesProxy.GetValue(index);

        public void SetValue(int index, object value) => _valuesProxy.SetValue(index, value);

        public Type GetObjectType() => _interfaceType;
    }
}
