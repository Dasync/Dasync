using System;
using Dasync.Modeling;

namespace Dasync.EETypes.Resolvers
{
    public interface IMethodResolver
    {
        bool TryResolve(IServiceDefinition serviceDefinition, MethodId methodId, out IMethodReference methodReference);
    }

    public static class MethodResolverExtensions
    {
        public static IMethodReference Resolve(this IMethodResolver resolver, IServiceDefinition serviceDefinition, MethodId methodId)
        {
            if (resolver.TryResolve(serviceDefinition, methodId, out var methodReference))
                return methodReference;
            throw new MethodResolveException(serviceDefinition.Name, methodId);
        }
    }

    public class MethodResolveException : Exception
    {
        public MethodResolveException()
            : base("Could not resolve a method.")
        {
        }

        public MethodResolveException(string serviceName, MethodId methodId)
            : base($"Could not resolve method '{methodId.Name}' in service '{serviceName}'.")
        {
            ServiceName = serviceName;
            MethodId = methodId;
        }

        public string ServiceName { get; }

        public MethodId MethodId { get; }
    }
}