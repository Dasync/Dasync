using System;
using System.Linq;
using Dasync.DependencyInjection;
using Dasync.Serialization;
using Dasync.ValueContainer;
using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.Serialization
{
    public class DbContextSerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly KnownDbContextTypes _knownDbContextTypes;
        private readonly IScopedServiceProvider _scopedServiceProvider;

        public DbContextSerializer(KnownDbContextTypes knownDbContextTypes, IScopedServiceProvider scopedServiceProvider)
        {
            _knownDbContextTypes = knownDbContextTypes;
            _scopedServiceProvider = scopedServiceProvider;
        }

        public bool CanSerialize(Type type) => typeof(DbContext).IsAssignableFrom(type);

        public bool CanDeserialize(Type type) => typeof(DbContext).IsAssignableFrom(type);

        public IValueContainer Decompose(object value)
        {
            var dbContextType = GetDbContextType(value.GetType());
            return new DbContextContainer
            {
                Type = dbContextType.FullName
            };
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new DbContextContainer();
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var dbContextContainer = (DbContextContainer)container;
            var targetDbContextType = GetDbContextType(valueType);
            if (targetDbContextType.FullName != dbContextContainer.Type)
                targetDbContextType = _knownDbContextTypes.Types.SingleOrDefault(t => t.FullName == dbContextContainer.Type);
            if (targetDbContextType == null)
                throw new InvalidOperationException($"Cannot find a known DbContext of type '{dbContextContainer.Type}'.");
            return _scopedServiceProvider.GetService(targetDbContextType);
        }

        public static Type GetDbContextType(DbContext dbContext) =>
            GetDbContextType(dbContext.GetType());

        public static Type GetDbContextType(Type type)
        {
            while (type.Assembly.IsDynamic)
                type = type.BaseType;
            return type;
        }
    }

    public sealed class DbContextContainer : ValueContainerBase, IValueContainerWithTypeInfo
    {
        public string Type;

        public Type GetObjectType() => typeof(DbContext);
    }
}
