using System;
using System.Reflection;

namespace Dasync.Serializers.DomainTypes.Projections
{
    public static class TypeInfoExtensions
    {
        public static bool IsProjectionInterface(this Type type) => IsProjectionInterface(type.GetTypeInfo());

        public static bool IsProjectionInterface(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));

            if (!typeInfo.IsInterface)
                return false;

            if (!typeInfo.IsPublic)
                return false;

            foreach (var interfaceType in typeInfo.GetInterfaces())
            {
                if (!interfaceType.IsProjectionInterface())
                    return false;
            }

            foreach (var member in typeInfo.GetMembers())
            {
                if (member.MemberType == MemberTypes.Method)
                {
                    var methodInfo = (MethodInfo)member;
                    if (!methodInfo.IsSpecialName)
                        return false;

                    continue;
                }

                if (member.MemberType != MemberTypes.Property)
                    return false;

                var propertyInfo = (PropertyInfo)member;

                if (propertyInfo.SetMethod != null)
                    return false;

                if (propertyInfo.GetMethod == null)
                    return false;
            }

            return true;
        }
    }
}
