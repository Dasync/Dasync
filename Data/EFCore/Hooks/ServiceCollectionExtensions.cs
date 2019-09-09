using System;
using System.Collections.Generic;
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
                if (typeof(DbContext).IsAssignableFrom(descriptor.ServiceType))
                    serviceDescriptors.Add(descriptor);
            }

            foreach (var descriptor in serviceDescriptors)
            {
                var dbContextType = descriptor.ServiceType;
                var dbContextProxyType = DbContextProxy.CreateDbContextProxyType(dbContextType);

                object ProvideDbContext(IServiceProvider sp)
                {
                    var dbContext = (DbContext)sp.GetService(dbContextProxyType);
                    var proxy = (IDbContextProxy)dbContext;

                    var decorators = sp.GetServices<IDbContextDecorator>();
                    foreach (var decorator in decorators)
                        decorator.Decorate(proxy);

                    var monitors = sp.GetServices<IDbContextMonitor>();
                    foreach (var monitor in monitors)
                        monitor.OnDbContextCreated(dbContext);

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
