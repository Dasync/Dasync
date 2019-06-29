using System;
using System.Collections.Generic;

namespace Dasync.Modeling
{
    public class PropertyBag : IMutablePropertyBag, IPropertyBag
    {
        private readonly SortedDictionary<string, Property> _properties
            = new SortedDictionary<string, Property>(StringComparer.OrdinalIgnoreCase);

        public Property FindProperty(string name) =>
            _properties.TryGetValue(name, out var property) ? property : null;

        public void AddProperty(string name, object value) =>
            _properties.Add(name, new Property(name, value));

        public void SetProperty(string name, object value)
        {
            if (value == null)
            {
                _properties.Remove(name);
            }
            else if (_properties.TryGetValue(name, out var property))
            {
                property.Value = value;
            }
            else
            {
                _properties.Add(name, new Property(name, value));
            }
        }

        public bool RemoveProperty(string name) =>
            _properties.Remove(name);

        public object this[string name]
        {
            get => FindProperty(name)?.Value;
            set => SetProperty(name, value);
        }

        IProperty IPropertyBag.FindProperty(string name) => FindProperty(name);
    }
}
