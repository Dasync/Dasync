using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dasync.ValueContainer
{
    public struct NamedValue
    {
        public string Name;
        public object Value;
        public Type Type;
        public int Index;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out string name, out object value)
        {
            name = Name;
            value = Value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out string name, out object value, out Type type)
        {
            name = Name;
            value = Value;
            type = Type;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out string name, out object value, out Type type, out int index)
        {
            name = Name;
            value = Value;
            type = Type;
            index = Index;
        }

        public static implicit operator KeyValuePair<string, object>(NamedValue namedValue)
            => new KeyValuePair<string, object>(namedValue.Name, namedValue.Value);
    }
}
