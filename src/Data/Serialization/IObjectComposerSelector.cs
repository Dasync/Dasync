using System;

namespace Dasync.Serialization
{
    public interface IObjectComposerSelector
    {
        IObjectComposer SelectComposer(Type targetType);
    }
}
