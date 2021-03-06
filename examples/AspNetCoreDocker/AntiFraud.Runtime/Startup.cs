using AntiFraud.Domain;
using Dasync.DependencyInjection;
using Dasync.Modeling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Contract;

namespace AntiFraud.Runtime
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private bool IsDevelopmentEnvironment =>
            Configuration.IsDevelopment() ||
            Configuration.IsEnvironment("IISExpress");

        public void ConfigureServices(IServiceCollection services)
        {
            // Describe services: one local and one external
            var communicationModel = CommunicationModelBuilder.Build(m => m
                .Service<AntiFraudService>(s => { })
                .ExternalService<IUsersService>(s => { }));

            // Plug in D-ASYNC infrastructure
            services.AddDasyncForAspNetCore(communicationModel, IsDevelopmentEnvironment);

            // Add several options to play with
            services.AddModules(
                Dasync.Communication.RabbitMQ.DI.Bindings,
                Dasync.Communication.InMemory.DI.Bindings,
                Dasync.Persistence.Cassandra.DI.Bindings,
                Dasync.Persistence.InMemory.DI.Bindings);
        }

        public void Configure(IApplicationBuilder app)
        {
            // Tell D-ASYNC to handle HTTP requests
            app.UseDasync(IsDevelopmentEnvironment);
        }
    }
}
