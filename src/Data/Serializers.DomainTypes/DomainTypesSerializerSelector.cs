using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.Modeling;
using Dasync.Serialization;
using Dasync.Serializers.DomainTypes.Projections;

namespace Dasync.Serializers.DomainTypes
{
    public sealed class DomainTypesSerializerSelector : IObjectDecomposerSelector, IObjectComposerSelector
    {
        private readonly HashSet<Type> _knownEntityProjectionInterfaces;
        private readonly EntityProjectionSerializer _entityProjectionSerializer;

        public DomainTypesSerializerSelector(
            ICommunicationModel communicationModel,
            EntityProjectionSerializer entityProjectionSerializer)
        {
            _knownEntityProjectionInterfaces = new HashSet<Type>(
                communicationModel.EntityProjections
                .Select(d => d.InterfaceType)
                .Where(i => EntityProjection.IsProjectionInterface(i)));

            _entityProjectionSerializer = entityProjectionSerializer;
            _entityProjectionSerializer.KnownEntityProjectionInterfaces = _knownEntityProjectionInterfaces;
        }

        public IObjectDecomposer SelectDecomposer(Type valueType)
        {
            if (valueType is EntityProjectionBase)
                return _entityProjectionSerializer;

            if (valueType.GetInterfaces().Any(i => _knownEntityProjectionInterfaces.Contains(i)))
                return _entityProjectionSerializer;

            return null;
        }

        public IObjectComposer SelectComposer(Type targetType)
        {
            if (typeof(EntityProjectionBase).IsAssignableFrom(targetType))
                return _entityProjectionSerializer;

            return null;
        }
    }
}
