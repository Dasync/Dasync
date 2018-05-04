using System;
using System.Reflection;

namespace Dasync.ValueContainer
{
    public struct EmptyValueContainer : IValueContainer, IStronglyTypedValueContainer
    {
        public int GetCount() => 0;

        public string GetName(int index) =>
            throw new InvalidOperationException("This value container is empty");

        public Type GetType(int index) =>
            throw new InvalidOperationException("This value container is empty");

        public object GetValue(int index) =>
            throw new InvalidOperationException("This value container is empty");

        public void SetValue(int index, object value) =>
            throw new InvalidOperationException("This value container is empty");

        public MemberInfo GetMember(int index) =>
            throw new InvalidOperationException("This value container is empty");
    }
}
