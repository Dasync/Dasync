using Dasync.DependencyInjection;
using Dasync.Modeling;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDasyncInMemoryEmulation(
            this IServiceCollection services,
            ICommunicationModel model = null)
        {
            services.AddModules(
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.DasyncJson.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Serializers.DomainTypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings,
                Dasync.Communication.InMemory.DI.Bindings,
                Dasync.Persistence.InMemory.DI.Bindings);

            if (model != null)
            {
                services.AddCommunicationModel(model);
                services.AddDomainServicesViaDasync(model);
            }

            return services;
        }
    }
}
