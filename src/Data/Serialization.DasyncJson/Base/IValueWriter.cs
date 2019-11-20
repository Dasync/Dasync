using System;

namespace Dasync.Serialization
{
    public interface IValueWriter : IDisposable
    {
        void WriteStart();
        void WriteStartValue(ValueInfo info);
        bool CanWriteValueWithoutTypeInfo(Type type, object value);
        void WriteValue(object value);
        void WriteEndValue();
        void WriteEnd();
    }
}
