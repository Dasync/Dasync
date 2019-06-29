using System;
using System.Collections.Generic;
using Dasync.Serialization;
using Dasync.Serializers.DomainTypes.Projections;

namespace Dasync.Serializers.DomainTypes
{
    public class DomainTypesNameShortener : ITypeNameShortener
    {
        private static readonly Dictionary<Type, string> _typeToNameMap = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Type> _nameToTypeMap = new Dictionary<string, Type>();

        static DomainTypesNameShortener()
        {
            RegisterType(typeof(EntityProjectionBase), "EntityProjection");
        }

        static void RegisterType(Type type, string shortName)
        {
            _typeToNameMap.Add(type, shortName);
            _nameToTypeMap.Add(shortName, type);
        }

        public bool TryShorten(Type type, out string shortName) => _typeToNameMap.TryGetValue(type, out shortName);

        public bool TryExpand(string shortName, out Type type) => _nameToTypeMap.TryGetValue(shortName, out type);
    }
}
