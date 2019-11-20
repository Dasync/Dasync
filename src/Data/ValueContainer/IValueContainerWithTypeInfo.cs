using System;

namespace Dasync.ValueContainer
{
    public interface IValueContainerWithTypeInfo
    {
        Type GetObjectType();
    }
}
