using System;
using System.Collections.Generic;

namespace Dasync.ValueContainer
{
    public sealed class ValueContainer : List<NamedValue>, IValueContainer
    {
        public int GetCount() => Count;

        public string GetName(int index) => base[index].Name;

        public Type GetType(int index) => base[index].Type;

        public object GetValue(int index) => base[index].Value;

        public void SetValue(int index, object value)
        {
            var namedValue = base[index];
            namedValue.Value = value;
            base[index] = namedValue;
        }

        public object this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                for (var i = 0; i < Count; i++)
                {
                    var namedValue = base[i];
                    if (string.Equals(namedValue.Name, name, StringComparison.Ordinal))
                        return namedValue.Value;
                }

                return null;
            }
            set
            {
                if (string.IsNullOrEmpty(name))
                    return;

                for (var i = 0; i < Count; i++)
                {
                    var namedValue = base[i];
                    if (string.Equals(namedValue.Name, name, StringComparison.Ordinal))
                    {
                        namedValue.Value = value;
                        base[i] = namedValue;
                        return;
                    }
                }

                if (value != null)
                {
                    Add(new NamedValue
                    {
                        Name = name,
                        Index = Count,
                        Type = value.GetType(),
                        Value = value
                    });
                }
            }
        }
    }
}
