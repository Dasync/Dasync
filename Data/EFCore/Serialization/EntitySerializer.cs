using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Dasync.DependencyInjection;
using Dasync.Serialization;
using Dasync.ValueContainer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dasync.EntityFrameworkCore.Serialization
{
    public class EntitySerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly IKnownDbContextSet _knownDbContextSet;
        private readonly IScopedServiceProvider _scopedServiceProvider;
        private readonly object _initializeLock = new object();
        private bool _isInitialized;
        private readonly Dictionary<Type, IEntityType> _entityTypes = new Dictionary<Type, IEntityType>();
        private readonly Dictionary<Type, Type> _entityToContextTypeMap = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, IValueContainerProxyFactory> _entityTypeToProxyFactoryMap = new Dictionary<Type, IValueContainerProxyFactory>();

        public EntitySerializer(IScopedServiceProvider scopedServiceProvider, IKnownDbContextSet knownDbContextSet)
        {
            _knownDbContextSet = knownDbContextSet;
            _scopedServiceProvider = scopedServiceProvider;
        }

        public bool CanSerialize(Type type)
        {
            SafeInitialize();
            return _entityTypes.ContainsKey(type);
        }

        public bool CanDeserialize(Type type)
        {
            SafeInitialize();
            return _entityTypes.ContainsKey(type);
        }

        public IValueContainer Decompose(object value)
        {
            var entityClrType = value.GetType();
            var keysProxy = _entityTypeToProxyFactoryMap[entityClrType].Create(value);
            return new EntityKeyContainer(entityClrType, keysProxy);
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            var entityClrType = valueType;
            var keysProxy = _entityTypeToProxyFactoryMap[entityClrType].Create(FormatterServices.GetUninitializedObject(entityClrType));
            return new EntityKeyContainer(entityClrType, keysProxy);
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var entityKeyContainer = (EntityKeyContainer)container;
            var entityClrType = entityKeyContainer.GetObjectType();
            var dbContextType = _entityToContextTypeMap[entityClrType];
            var dbContext = (DbContext)_scopedServiceProvider.GetService(dbContextType);
            var entity = dbContext.Find(entityClrType, entityKeyContainer.GetValues());
            // What if an entity does not exist anymore? Is returning NULL ok?
            return entity;
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

                    var keysProxyFactory = GetKeysProxyFactory(entityType);
                    if (keysProxyFactory == null)
                        return;

                    _entityTypes.Add(entityType.ClrType, entityType);
                    _entityToContextTypeMap.Add(entityType.ClrType, dbContextType);
                    _entityTypeToProxyFactoryMap.Add(entityType.ClrType, keysProxyFactory);
                }
            }
        }

        private static IValueContainerProxyFactory GetKeysProxyFactory(IEntityType entityType)
        {
            var pk = FindPrimaryKey(entityType);
            if (pk == null)
                return null;

            var pkProps = new List<KeyValuePair<string, MemberInfo>>();
            foreach (var prop in pk.Properties)
                pkProps.Add(new KeyValuePair<string, MemberInfo>(
                    prop.Name, (MemberInfo)prop.FieldInfo ?? prop.PropertyInfo));

            return ValueContainerFactory.GetProxyFactory(entityType.ClrType, pkProps);
        }

        private static IKey FindPrimaryKey(IEntityType entityType)
        {
            return entityType.GetKeys().FirstOrDefault();
        }
    }

    public sealed class EntityKeyContainer : IValueContainer, IValueContainerWithTypeInfo
    {
        private Type _entityClrType;
        private IValueContainer _keysProxy;

        public EntityKeyContainer(Type entityClrType, IValueContainer keysProxy)
        {
            _entityClrType = entityClrType;
            _keysProxy = keysProxy;
        }

        public int GetCount() => _keysProxy.GetCount();

        public string GetName(int index) => _keysProxy.GetName(index);

        public Type GetType(int index) => _keysProxy.GetType(index);

        public object GetValue(int index) => _keysProxy.GetValue(index);

        public void SetValue(int index, object value) => _keysProxy.SetValue(index, value);

        public Type GetObjectType() => _entityClrType;
    }
}
