using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.DependencyInjection;
using Dasync.EntityFrameworkCore.Hooks;
using Dasync.EntityFrameworkCore.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEFCoreUnitOfWork(this IServiceCollection services)
        {
            services.AddEFCoreHooks();
            services.AddDbContextProviders();
            services.AddModule(DI.Bindings);
            return services;
        }

        private static IServiceCollection AddDbContextProviders(this IServiceCollection services)
        {
            var serviceDescriptors = new List<ServiceDescriptor>();
            foreach (var descriptor in services)
            {
                if (!descriptor.ServiceType.Assembly.IsDynamic && typeof(DbContext).IsAssignableFrom(descriptor.ServiceType))
                    serviceDescriptors.Add(descriptor);
            }

            foreach (var descriptor in serviceDescriptors)
            {
                var dbContextType = descriptor.ServiceType;

                services.AddSingleton(
                    typeof(ICurrentDbContext<>).MakeGenericType(dbContextType),
                    typeof(CurrentDbContextProvider<>).MakeGenericType(dbContextType));

                services.AddSingleton(
                    typeof(Func<>).MakeGenericType(dbContextType),
                    sp => ProvideDbContextMethodInfo.MakeGenericMethod(dbContextType).Invoke(null, new object[] { sp }));
            }

            services.AddSingleton(new KnownDbContextTypes(serviceDescriptors.Select(d => d.ServiceType)));

            return services;
        }

        private static Func<TContext> ProvideDbContext<TContext>(IServiceProvider sp) where TContext : DbContext
        {
            return () => sp.GetService<ICurrentDbContext<TContext>>().Instance;
        }

        private static readonly MethodInfo ProvideDbContextMethodInfo =
            typeof(ServiceCollectionExtensions).GetMethod(nameof(ProvideDbContext), BindingFlags.Static | BindingFlags.NonPublic);
    }
}
