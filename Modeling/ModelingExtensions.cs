using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Modeling
{
    public static class ModelingExtensions
    {
        public static bool IsRoutineCandidate(this MethodInfo methodInfo)
        {
            if (!methodInfo.IsFamily && !methodInfo.IsPublic)
                return false;

            if (!methodInfo.IsVirtual)
                return false;

            if (methodInfo.IsAbstract && !methodInfo.DeclaringType.IsInterface)
                return false;

            if (methodInfo.ContainsGenericParameters)
                return false;

            if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                // An exception from the rule for service instances.
                if (methodInfo.ReturnType == typeof(void) &&
                    methodInfo.Name == nameof(IDisposable.Dispose) &&
                    methodInfo.GetParameters().Length == 0)
                    return true;

                return false;
            }

            return true;
        }
    }
}
