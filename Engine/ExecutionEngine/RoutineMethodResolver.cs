using System;
using System.Linq;
using System.Reflection;
using Dasync.EETypes;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine
{
    public interface IRoutineMethodResolver
    {
        MethodInfo Resolve(IServiceDefinition serviceDefinition, RoutineMethodId methodId);
    }

    public class RoutineMethodResolver : IRoutineMethodResolver
    {
        private readonly ICommunicationModelProvider _communicationModelProvider;

        public RoutineMethodResolver(ICommunicationModelProvider communicationModelProvider)
        {
            _communicationModelProvider = communicationModelProvider;
        }

        public MethodInfo Resolve(IServiceDefinition serviceDefinition, RoutineMethodId methodId)
        {
#warning Should use Service interface type to get the interface mapping to resolve the proper method?
#warning Must use versioning to redirect to a different method.

            var serviceType = serviceDefinition.Implementation;
            if (serviceType == null)
                throw new InvalidOperationException($"Impossible to resolve a method implementation on the external service '{serviceDefinition.Name}'.");

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
                throw new MissingMethodException($"The service '{serviceDefinition.Name}' does not have a method '{methodId.MethodName}'.");

            return method;
        }
    }
}
