using System;

namespace Dasync.Serialization
{
    public interface IValueReader : IDisposable
    {
        void Read(IObjectReconstructor reconstructor, ISerializer serializer);
    }
}
