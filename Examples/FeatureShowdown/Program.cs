using System;
using System.Text;
using System.Threading.Tasks;
using Dasync.Bootstrap;
using Dasync.Ioc;
using Dasync.Ioc.Ninject;
using Ninject;

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
            await PlugInDasync(feature.AppKernel, inMemoryEmulation: false);

            await feature.Run();
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

        static async Task PlugInDasync(IKernel appKernel, bool inMemoryEmulation)
        {
            var engineContainer = new BasicIocContainer()
                .Load(Dasync.Ioc.DI.Bindings)
                .Load(Dasync.Serialization.DI.Bindings)
                .Load(Dasync.Serialization.Json.DI.Bindings)
                .Load(Dasync.ServiceRegistry.DI.Bindings)
                .Load(Dasync.Proxy.DI.Bindings)
                .Load(Dasync.AsyncStateMachine.DI.Bindings)
                .Load(Dasync.ExecutionEngine.DI.Bindings)
                .Load(Dasync.Bootstrap.DI.Bindings);

            if (inMemoryEmulation)
                engineContainer.Load(Dasync.Fabric.InMemory.DI.Bindings);
            else
                engineContainer.Load(Dasync.Fabric.FileBased.DI.Bindings);

            engineContainer.Bind(typeof(IAppIocContainerProvider),
                new ConstantAppIocContainerProvider(appKernel.ToIocContainer()));

            await engineContainer.Resolve<Bootstrapper>().BootstrapAsync(default);
        }
    }

    public interface IFeatureDemo
    {
        string Name { get; }

        IKernel AppKernel { get; }

        Task Run();
    }
}
