using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.AsyncStateMachine
{
    public static class MethodInfoToStateMachineTypeConverter
    {
        public static Type GetStateMachineType(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

#warning cache?
            var attr = methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (attr == null)
                throw new ArgumentException($"The method '{methodInfo.Name}' of type '{methodInfo.DeclaringType.Name}'" +
                    $" is not marked with the {nameof(AsyncStateMachineAttribute)}", nameof(methodInfo));

            return attr.StateMachineType;
        }
    }
}
