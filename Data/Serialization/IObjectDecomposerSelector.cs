using System;

namespace Dasync.Serialization
{
    public interface IObjectDecomposerSelector
    {
        IObjectDecomposer SelectDecomposer(Type valueType);
    }
}
