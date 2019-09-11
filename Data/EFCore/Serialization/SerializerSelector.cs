using System;
using Dasync.Serialization;

namespace Dasync.EntityFrameworkCore.Serialization
{
    public class SerializerSelector : IObjectDecomposerSelector, IObjectComposerSelector
    {
        private readonly EntitySerializer _entitySerializer;
        private readonly EntityProjectionSerializer _entityProjectionSerializer;
        private readonly DbContextSerializer _dbContextSerializer;

        public SerializerSelector(
            EntityProjectionSerializer entityProjectionSerializer,
            EntitySerializer entitySerializer,
            DbContextSerializer dbContextSerializer)
        {
            _entitySerializer = entitySerializer;
            _entityProjectionSerializer = entityProjectionSerializer;
            _dbContextSerializer = dbContextSerializer;
        }

        public IObjectDecomposer SelectDecomposer(Type valueType)
        {
            if (_entitySerializer.CanSerialize(valueType))
                return _entitySerializer;

            if (_entityProjectionSerializer.CanSerialize(valueType))
                return _entityProjectionSerializer;

            if (_dbContextSerializer.CanSerialize(valueType))
                return _dbContextSerializer;

            return null;
        }

        public IObjectComposer SelectComposer(Type targetType)
        {
            if (_entitySerializer.CanDeserialize(targetType))
                return _entitySerializer;

            if (_entityProjectionSerializer.CanDeserialize(targetType))
                return _entityProjectionSerializer;

            if (_dbContextSerializer.CanDeserialize(targetType))
                return _dbContextSerializer;

            return null;
        }
    }
}
