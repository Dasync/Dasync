using System;
using Dasync.Modeling;

namespace Dasync.EETypes.Resolvers
{
    public interface IMethodResolver
    {
        bool TryResolve(IServiceDefinition serviceDefinition, RoutineMethodId methodId, out IMethodReference methodReference);
    }

    public static class MethodResolverExtensions
    {
        public static IMethodReference Resolve(this IMethodResolver resolver, IServiceDefinition serviceDefinition, RoutineMethodId methodId)
        {
            if (resolver.TryResolve(serviceDefinition, methodId, out var methodReference))
                return methodReference;
            throw new InvalidOperationException($"Could not resolve method '{methodId.Name}' in service '{serviceDefinition.Name}'.");
        }
    }
}