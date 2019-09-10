using System;
using System.Collections.Generic;
using Dasync.Serialization;
using Dasync.Serializers.DomainTypes.Projections;

namespace Dasync.Serializers.DomainTypes
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ITypeNameShortener)] = typeof(DomainTypesNameShortener),
            [typeof(DomainTypesSerializerSelector)] = typeof(DomainTypesSerializerSelector),
            [typeof(IObjectDecomposerSelector)] = typeof(DomainTypesSerializerSelector),
            [typeof(IObjectComposerSelector)] = typeof(DomainTypesSerializerSelector),
            [typeof(EntityProjectionSerializer)] = typeof(EntityProjectionSerializer),
        };
    }
}
