using System;

namespace Dasync.Serialization
{
    public class TypeResolver : ITypeResolver
    {
        private readonly IAssemblyResolver _assemblyResolver;

        public TypeResolver(IAssemblyResolver assemblyResolver)
        {
            _assemblyResolver = assemblyResolver;
        }

        public Type Resolve(TypeSerializationInfo info)
        {
            if (string.IsNullOrEmpty(info.Name))
                throw new ArgumentException($"Empty {nameof(TypeSerializationInfo)}.{nameof(TypeSerializationInfo.Name)}");

#warning Cache results

            Type type;
            if (info.Assembly != null)
            {
                var assembly = _assemblyResolver.Resolve(info.Assembly);
                type = assembly.GetType(info.Name, throwOnError: true, ignoreCase: false);
            }
            else
            {
                type = Type.GetType(info.Name, throwOnError: true, ignoreCase: false);
            }

            if (info.GenericArgs?.Length > 0)
            {
                if (!type.IsGenericTypeDefinition())
                    throw new InvalidOperationException(
                        $"The type '{type}' is not a generic type definition.");

                var genericArguments = new Type[info.GenericArgs.Length];
                for (var i = 0; i < genericArguments.Length; i++)
                    genericArguments[i] = Resolve(info.GenericArgs[i]);

                type = type.MakeGenericType(genericArguments);
            }

            return type;
        }
    }
}
