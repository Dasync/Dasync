using System;

namespace Dasync.Serialization
{
    public interface IValueWriter : IDisposable
    {
        void WriteStart();
        void WriteStartValue(ValueInfo info);
        bool CanWriteValueWithoutTypeInfo(Type type);
        void WriteValue(object value);
        void WriteEndValue();
        void WriteEnd();
    }
}
