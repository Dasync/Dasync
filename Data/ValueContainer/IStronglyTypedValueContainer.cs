using System.Reflection;

namespace Dasync.ValueContainer
{
    /// <summary>
    /// Such value container exposes all of its members as properties or/and fields,
    /// so you can compile dynamic code to access values for the maximum performance.
    /// </summary>
    public interface IStronglyTypedValueContainer : IValueContainer
    {
        MemberInfo GetMember(int index);
    }
}
