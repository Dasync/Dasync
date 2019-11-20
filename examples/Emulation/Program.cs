using System;
using System.Text;
using System.Threading.Tasks;
using Dasync.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DasyncFeatures
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("DASYNC Feature Showdown (emulated in-memory)");
            Console.WriteLine("Look at the code and its comments to understand the demo");
            Console.WriteLine();

            // This app contains several scenarios that demonstrates capabilities of the D-ASYNC
            // engine to execute methods and react to events as if it was (were) a cloud service(s).

            // The code for every single feature is very simple and does not utilize any framework,
            // however the D-ASYNC engine plugs into .NET's runtime to control execution of methods
            // what makes it possible to serialize their input, output, and state.

            // The ability to plug into the runtime inverts the dependency - there is no need for a
            // specific framework, but instead you use the programming language to express what the
            // application should do and configure infrastructure later to tell how it should be done.

            // In this demo the serialized data is conveyed by an in-memory emulator, which can be
            // replaced by cloud services (http, message queues, event streams, etc.) to run the
            // business logic code in resilient, scalable, and distributed manner without modification.

            var featureDemoSet = new IFeatureDemo[]
            {
                new Feature1.Demo(),
                new Feature2.Demo(),
                new Feature3.Demo(),
                new Feature4.Demo(),
                new Feature5.Demo(),
                new Feature6.Demo(),
                new Feature7.Demo(),
            };

            var feature = SelectFeature(featureDemoSet);

            using (var host = new HostBuilder()
                .ConfigureAppConfiguration(_ => _
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args))
                .ConfigureServices(services =>
                        PlugInDasync(services, feature))
                .Start())
            {
                await feature.Run(host.Services);
                await host.StopAsync();
                await host.WaitForShutdownAsync();
            }
        }

        static IFeatureDemo SelectFeature(IFeatureDemo[] features)
        {
            var legend = new StringBuilder();
            legend.AppendLine("Available features to run:");
            for (var i = 0; i < features.Length; i++)
                legend.AppendLine($"{i + 1}. {features[i].Name}");
            Console.WriteLine(legend.ToString());

            var featureIndex = -1;
            do
            {
                Console.Write("Select feature #: ");
                var selection = Console.ReadLine();
                if (int.TryParse(selection, out featureIndex)
                    && featureIndex >= 1
                    && featureIndex <= features.Length)
                    featureIndex--;
                else
                    featureIndex = -1;
            }
            while (featureIndex < 0);

            Console.WriteLine();

            return features[featureIndex];
        }

        static void PlugInDasync(IServiceCollection services, IFeatureDemo feature)
        {
            services.AddDasyncInMemoryEmulation(feature.Model);
            services.AddModule(feature.Bindings);
        }
    }
}
