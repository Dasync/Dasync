using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dasync.DependencyInjection;
using Dasync.EETypes.Ioc;
using Dasync.Fabric.Sample.Base;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures
{
    public class Program
    {
        static async Task Main()
        {
            Console.WriteLine("DASYNC Feature Showdown [TECH PREVIEW]");
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

            var feature = SelectFeature(featureDemoSet);

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
            var services = new ServiceCollection();
            PlugInDasync(services, feature, inMemoryEmulation: false);

            var serviceProvider = services.BuildServiceProvider();
            StartFabric(serviceProvider);
            await feature.Run(serviceProvider);
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

        static void PlugInDasync(IServiceCollection services, IFeatureDemo feature, bool inMemoryEmulation)
        {
            services.AddModules(
                Dasync.Modeling.DI.Bindings,
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.Json.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Serializers.DomainTypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings);

            services.AddSingleton<IDomainServiceProvider, DomainServiceProvider>();

            services.AddModule(Dasync.Fabric.Sample.Base.DI.Bindings);
            if (inMemoryEmulation)
                services.AddModule(Dasync.Fabric.InMemory.DI.Bindings);
            else
                services.AddModule(Dasync.Fabric.FileBased.DI.Bindings);

            services.Rebind<ICommunicationModelProvider>().To(new CommunicationModelProvider(
                new CommunicationModelProvider.Holder { Model = feature.Model }));

            services.AddModule(feature.Bindings);
            services.AddDomainServicesViaDasync(feature.Model);
        }

        static void StartFabric(IServiceProvider services)
        {
            var communicationModelProvider = services.GetService<ICommunicationModelProvider>();
            var domainServiceProvider = services.GetService<IDomainServiceProvider>();

            var fabric = services.GetService<IFabric>();
            fabric.InitializeAsync(default).Wait();

            services.GetService<ICurrentFabricSetter>().SetInstance(fabric);

            // ResolveAllDomainServices
            var communicationModel = communicationModelProvider.Model;
            foreach (var serviceDefinition in communicationModel.Services)
            {
                if (serviceDefinition.Implementation != null)
                {
                    domainServiceProvider.GetService(serviceDefinition.Implementation);
                }

                if (serviceDefinition.Interfaces?.Length > 0)
                {
                    foreach (var interfaceType in serviceDefinition.Interfaces)
                    {
                        domainServiceProvider.GetService(interfaceType);
                    }
                }
            }

            fabric.StartAsync(default).Wait();
        }
    }
}
