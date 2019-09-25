using System;

namespace Dasync.Serialization
{
    [Obsolete]
    public interface ISerializerFactorySelector
    {
        [Obsolete]
        ISerializerFactory Select(string format);
    }
}
