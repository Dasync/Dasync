using System;

namespace Dasync.Serialization
{
    public interface ITypeResolver
    {
        Type Resolve(TypeSerializationInfo info);
    }
}
