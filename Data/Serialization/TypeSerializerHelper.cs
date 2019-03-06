using System;
using System.Linq;
using System.Reflection;

namespace Dasync.Serialization
{
    public interface ITypeSerializerHelper
    {
        TypeSerializationInfo GetTypeSerializationInfo(Type type);
        Type ResolveType(TypeSerializationInfo info);
    }

    public class TypeSerializerHelper : ITypeSerializerHelper
    {
        private readonly ITypeResolver _typeResolver;
        private readonly ITypeNameShortener _typeNameShortener;
        private readonly IAssemblyNameShortener _assemblyNameShortener;

        public TypeSerializerHelper(
            ITypeResolver typeResolver,
            ITypeNameShortener typeNameShortener,
            IAssemblyNameShortener assemblyNameShortener)
        {
            _typeResolver = typeResolver;
            _typeNameShortener = typeNameShortener;
            _assemblyNameShortener = assemblyNameShortener;
        }

        public TypeSerializationInfo GetTypeSerializationInfo(Type type)
        {
#warning Ignore types from dynamic assemblies

            if (_typeNameShortener.TryShorten(type, out var typeShortName))
            {
                return new TypeSerializationInfo
                {
                    Name = typeShortName
                };
            }
            else if (type.IsGenericType() && _typeNameShortener.TryShorten(
                type.GetGenericTypeDefinition(), out typeShortName))
            {
                return new TypeSerializationInfo
                {
                    Name = typeShortName,
                    GenericArgs = type.GetGenericArguments().Select(t => GetTypeSerializationInfo(t)).ToArray()
                };
            }
            else if (_assemblyNameShortener.TryShorten(type.GetAssembly(), out string assemblyShortName))
            {
                return new TypeSerializationInfo
                {
                    Name = type.GetFullName(),
                    Assembly = new AssemblySerializationInfo
                    {
                        Name = assemblyShortName
                    },
                    GenericArgs = type.IsGenericType()
                        ? type.GetGenericArguments().Select(t => GetTypeSerializationInfo(t)).ToArray()
                        : null
                };
            }
            else
            {
                return type.ToTypeSerializationInfo();
            }
        }

        public Type ResolveType(TypeSerializationInfo info)
        {
            if (!_typeNameShortener.TryExpand(info.Name, out Type type))
            {
                if (_assemblyNameShortener.TryExpand(info.Assembly?.Name, out Assembly assembly))
                    info.Assembly = assembly.ToAssemblySerializationInfo();

                var infoForResolving = info.GenericArgs?.Length > 0
                    ? new TypeSerializationInfo
                    {
                        Name = info.Name,
                        Assembly = info.Assembly
                    }
                    : info;
                type = _typeResolver.Resolve(infoForResolving);
            }

            if (type.IsGenericTypeDefinition())
            {
                var genericArguments = new Type[info.GenericArgs.Length];
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    var genericArgument = ResolveType(info.GenericArgs[i]);
                    genericArguments[i] = genericArgument;
                }
                type = type.MakeGenericType(genericArguments);
            }

            return type;
        }
    }
}
