using Dasync.AspNetCore.DependencyInjection;
using Dasync.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DasyncAspNetCore
{
    public static class ServiceCollectionDasyncExtensions
    {
        public static IServiceCollection AddDasync(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DasyncOptions>(configuration.GetSection("Dasync"));
            services.AddScoped<ScopedServiceProviderMiddleware>();
            services.AddScoped<DasyncMiddleware>();
            services.AddSingleton<IHttpRequestHandler, HttpRequestHandler>();
            services.AddSingleton<IHostedService, DasyncCoHost>();
            services.AddSingleton<IStartupFilter, DasyncStartupFilter>();

            services.AddModules(
                Dasync.DependencyInjection.DI.Bindings,
                Dasync.Modeling.DI.Bindings,
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.Json.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Serializers.DomainTypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings);

            services.AddModules(Dasync.AspNetCore.DI.Bindings);

            return services;
        }

        public static IServiceCollection AddDasyncWithSimpleHttpPlatform(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDasync(configuration);
            services.AddDasyncSimpleHttpPlatform();
            return services;
        }

        public static IServiceCollection AddDasyncSimpleHttpPlatform(this IServiceCollection services)
        {
            services.AddModules(Dasync.AspNetCore.Platform.DI.Bindings);
            return services;
        }
    }
}
