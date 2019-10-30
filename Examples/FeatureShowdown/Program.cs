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
            Console.WriteLine("DASYNC Feature Showdown");
            Console.WriteLine("Look at the code and its comments to understand the demo!");
            Console.WriteLine();

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

            //var feature = SelectFeature(featureDemoSet);
            var feature = featureDemoSet[0];

            // !!! LOOK HERE !!!
            //
            // There are 3 options to play with:
            //
            // 1. Comment out PlugInDasync to run a demo code without DASYNC
            //    as a regular .NET application.
            //
            // 2. Run im-memory emulation of DASYNC runtime to test
            //    serialization only.
            //
            // 3. Run with file-based persistence, so next time your re-start
            //    this console app, it will pick up any unfinished work. [DEFAULT]
            //    Where does it store the data? In the 'data' folder :)
            //    It should be under '/bin/Debug/netcoreapp2.0' directory.

            IHost host = new HostBuilder()
                .ConfigureAppConfiguration(_ => _
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args))
                .ConfigureServices(services =>
                        PlugInDasync(services, feature))
                .Start();

            await feature.Run(host.Services);
            await host.StopAsync();
            await host.WaitForShutdownAsync();
            host.Dispose();
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
            services.AddModules(
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.DasyncJson.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Serializers.DomainTypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings);

            services.AddModules(
                Dasync.Communication.InMemory.DI.Bindings,
                Dasync.Persistence.InMemory.DI.Bindings);

            services.AddModule(feature.Bindings);
            services.AddCommunicationModel(feature.Model);
            services.AddDomainServicesViaDasync(feature.Model);
        }
    }
}
