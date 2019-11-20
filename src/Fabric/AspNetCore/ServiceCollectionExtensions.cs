using Dasync.DependencyInjection;
using Dasync.Fabric.AspNetCore;
using Dasync.Modeling;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDasyncForAspNetCore(
            this IServiceCollection services,
            ICommunicationModel model = null,
            bool? isDevelopment = null)
        {
            services.AddModules(
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.DasyncJson.DI.Bindings,
                Dasync.Serialization.Json.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Serializers.DomainTypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings,
                Dasync.Communication.Http.DI.Bindings,
                Dasync.Hosting.AspNetCore.DI.Bindings);

            if (isDevelopment == true || (!isDevelopment.HasValue && AspNetCoreEnvironment.IsDevelopment))
            {
                services.AddModules(Dasync.Hosting.AspNetCore.Development.DI.Bindings);
            }

            if (model != null)
            {
                services.AddCommunicationModel(model);
                services.AddDomainServicesViaDasync(model);
            }

            return services;
        }
    }
}
