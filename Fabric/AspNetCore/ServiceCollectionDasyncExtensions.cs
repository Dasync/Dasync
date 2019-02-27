using System;
using Dasync.EETypes;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Proxy;
using Dasync.Modeling;
using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IStartupFilter = Microsoft.AspNetCore.Hosting.IStartupFilter;

namespace DasyncAspNetCore
{
    public static class ServiceCollectionDasyncExtensions
    {
        public static IServiceCollection AddDasync(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DasyncOptions>(configuration.GetSection("Dasync"));
            services.AddTransient<DasyncMiddleware>();
            services.AddSingleton<IHttpRequestHandler, HttpRequestHandler>();
            services.AddSingleton<IHostedService, DasyncCoHost>();

            services.AddSingleton<IStartupFilter, TestStartupFilter>();

            services.AddModules(
                Dasync.Modeling.DI.Bindings,
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.Json.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings,
                Dasync.Bootstrap.DI.Bindings);

            services.AddModules(
                Dasync.Fabric.Sample.Base.DI.Bindings,
                Dasync.Fabric.InMemory.DI.Bindings);

            services.AddSingleton<IDomainServiceProvider, DomainServiceProvider>();

            return services;
        }

        public static IServiceCollection AddDomainServicesViaDasync(this IServiceCollection services, ICommunicationModel model)
        {
            foreach (var serviceDefinition in model.Services)
            {
                if (serviceDefinition.Implementation != null)
                {
                    services.Add(new ServiceDescriptor(
                        serviceDefinition.Implementation,
                        serviceProvider =>
                            serviceProvider
                            .GetService<IServiceProxyBuilder>()
                            .Build(new ServiceId { ServiceName = serviceDefinition.Name }),
                        ServiceLifetime.Singleton));
                }

                if (serviceDefinition.Interfaces != null)
                {
                    foreach (var interfaceType in serviceDefinition.Interfaces)
                    {
                        services.Add(new ServiceDescriptor(
                            interfaceType,
                            serviceProvider =>
                                serviceDefinition.Implementation != null
                                ? serviceProvider.GetService(serviceDefinition.Implementation)
                                : serviceProvider
                                .GetService<IServiceProxyBuilder>()
                                .Build(new ServiceId { ServiceName = serviceDefinition.Name }),
                            ServiceLifetime.Singleton));
                    }
                }
            }

            return services;
        }
    }

    public class TestStartupFilter : IStartupFilter
    {
        private readonly IApplicationLifetime _appLifetime;

        public TestStartupFilter(IApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;

            appLifetime.ApplicationStarted.Register(OnAppStarted, useSynchronizationContext: false);
        }

        private void OnAppStarted()
        {
            return;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return next;
        }
    }
}
