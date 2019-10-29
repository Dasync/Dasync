using System;

namespace Dasync.Serialization
{
    public interface IObjectReconstructor
    {
        Type GetExpectedValueType(string propertyName);
        void OnValueStart(ValueInfo info);
        void OnValue(object value);
        void OnValueEnd();
    }
}
