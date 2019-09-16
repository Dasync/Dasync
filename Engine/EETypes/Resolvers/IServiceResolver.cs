using System;

namespace Dasync.EETypes.Resolvers
{
    public interface IServiceResolver
    {
        bool TryResolve(ServiceId serviceId, out IServiceReference serviceReference);
    }

    public static class ServiceResolverExtensions
    {
        public static IServiceReference Resolve(this IServiceResolver resolver, ServiceId serviceId)
        {
            if (resolver.TryResolve(serviceId, out var serviceReference))
                return serviceReference;
            throw new InvalidOperationException($"Could not resolve service '{serviceId.Proxy ?? serviceId.Name}'.");
        }
    }
}
