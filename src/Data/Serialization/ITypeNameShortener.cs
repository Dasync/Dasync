using System;

namespace Dasync.Serialization
{
#warning Use TypeSerializationInfo instead of Type
    public interface ITypeNameShortener
    {
        bool TryShorten(Type type, out string shortName);

        bool TryExpand(string shortName, out Type type);
    }
}
