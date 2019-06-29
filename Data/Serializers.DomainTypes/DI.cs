using System;
using System.Collections.Generic;
using Dasync.Serializers.DomainTypes.Projections;

namespace Dasync.Serializers.DomainTypes
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(DomainTypesNameShortener)] = typeof(DomainTypesNameShortener),
            [typeof(DomainTypesSerializerSelector)] = typeof(DomainTypesSerializerSelector),
            [typeof(EntityProjectionSerializer)] = typeof(EntityProjectionSerializer),
        };
    }
}
