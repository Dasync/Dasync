using System;
using System.Collections.Generic;

namespace Dasync.Serialization
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IAssemblyResolver)] = typeof(AssemblyResolver),
            [typeof(ITypeResolver)] = typeof(TypeResolver),
            [typeof(ISerializerFactorySelector)] = typeof(SerializerFactorySelector),
            [typeof(IStandardSerializerFactory)] = typeof(StandardSerializerFactory),
            [typeof(ITypeSerializerHelper)] = typeof(TypeSerializerHelper),
            [typeof(ISerializerProvider)] = typeof(SerializerProvider),
            [typeof(IDefaultSerializerProvider)] = typeof(DefaultSerializerProvider),
        };
    }
}
