using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Dasync.ValueContainer
{
    public abstract class ValueContainerBase : IValueContainer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IValueContainer _impl;

        public ValueContainerBase()
        {
            var delegatedMembers = new List<MemberInfo>();
            foreach (var mi in GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                if (mi is FieldInfo fieldInfo)
                {
                    if (!fieldInfo.IsInitOnly && !fieldInfo.IsStatic)
                        delegatedMembers.Add(fieldInfo);
                    continue;
                }

                if (mi is PropertyInfo propertyInfo)
                {
                    if (propertyInfo.CanRead && propertyInfo.CanWrite && !propertyInfo.GetMethod.IsStatic)
                        delegatedMembers.Add(propertyInfo);
                    continue;
                }
            }

            var containerType = ValueContainerTypeBuilder.Build(GetType(), delegatedMembers);
            _impl = (IValueContainer)Activator.CreateInstance(containerType, new object[] { this });
        }

        public int GetCount() => _impl.GetCount();

        public string GetName(int index) => _impl.GetName(index);

        public Type GetType(int index) => _impl.GetType(index);

        public object GetValue(int index) => _impl.GetValue(index);

        public void SetValue(int index, object value) => _impl.SetValue(index, value);
    }
}
