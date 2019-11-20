using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.EntityFrameworkCore.Hooks
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEFCoreHooks(this IServiceCollection services)
        {
            var serviceDescriptors = new List<ServiceDescriptor>();
            foreach (var descriptor in services)
            {
                // Search for DbContext service types only
                if (!typeof(DbContext).IsAssignableFrom(descriptor.ServiceType))
                    continue;

                // Skip DbContexts that take in IDbContextEvents - assume manual invocation without proxies.
                if (descriptor.ServiceType.GetConstructors().Any(
                    ctor => ctor.GetParameters().Any(
                        p => p.ParameterType == typeof(IDbContextEvents))))
                    continue;

                serviceDescriptors.Add(descriptor);
            }

            foreach (var descriptor in serviceDescriptors)
            {
                var dbContextType = descriptor.ServiceType;
                var dbContextProxyType = DbContextProxy.CreateDbContextProxyType(dbContextType);

                object ProvideDbContext(IServiceProvider sp)
                {
                    var dbContextEvents = sp.GetService<IDbContextEvents>();
                    var dbContext = (DbContext)sp.GetService(dbContextProxyType);
                    var proxy = (IDbContextProxy)dbContext;
                    proxy.OnModelCreatingCallback += (modelBuilder) => dbContextEvents.OnModelCreating(dbContext, modelBuilder);
                    dbContextEvents.OnContextCreated(dbContext);
                    return dbContext;
                };

                services.Remove(descriptor);
                services.Add(new ServiceDescriptor(dbContextType, ProvideDbContext, descriptor.Lifetime));
                services.Add(new ServiceDescriptor(dbContextProxyType, dbContextProxyType, descriptor.Lifetime));
            }

            return services;
        }
    }
}
