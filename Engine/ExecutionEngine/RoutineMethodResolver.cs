using System;
using System.Linq;
using System.Reflection;
using Dasync.EETypes;

namespace Dasync.ExecutionEngine
{
    public interface IRoutineMethodResolver
    {
        MethodInfo Resolve(Type serviceType, RoutineMethodId methodId);
    }

    public class RoutineMethodResolver : IRoutineMethodResolver
    {
        public MethodInfo Resolve(Type serviceType, RoutineMethodId methodId)
        {
#warning Should use Service interface type to get the interface mapping to resolve the proper method?
#warning Must use versioning to redirect to a different method.

            var methods = serviceType.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
#warning if not found in declared on this type, go to base type
#warning also neede to check implemented interfaces if the input type is an interface
                BindingFlags.DeclaredOnly);

            var method = methods.SingleOrDefault(mi => mi.Name == methodId.MethodName);

            if (method == null)
                throw new Exception($"method '{methodId.MethodName}' not found on type '{serviceType.FullName}'");

            return method;
        }
    }
}
