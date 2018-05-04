using System;

namespace Dasync.ValueContainer
{
    public interface IValueContainer
    {
        int GetCount();

        string GetName(int index);

        Type GetType(int index);

        object GetValue(int index);

        void SetValue(int index, object value);
    }
}
