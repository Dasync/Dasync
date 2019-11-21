using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures.Feature4
{
    /// <summary>
    /// Demonstrates an integration with TPL 'WhenAll' function, that helps
    /// to run multiple routine in parallel in stateful manner. This feature
    /// enables the 'horizontal' scaling.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Parallel Routine Execution";

        public ICommunicationModel Model { get; } = CommunicationModelBuilder.Build(m => m.Service<BaristaWorker>(s => { }));

        public Dictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public async Task Run(IServiceProvider services)
        {
            var baristaWorker = services.GetService<IBaristaWorker>();
            await baristaWorker.PerformDuties();
        }
    }

    public interface IBaristaWorker
    {
        Task PerformDuties();
    }

    public class BaristaWorker : IBaristaWorker
    {
        public virtual async Task PerformDuties()
        {
            // WhenAll is translated into such series of steps:
            // 1. Save state of current method
            // 2. Schedule WelcomeGuest
            // 3. Schedule BrowseInternet
            // 4. WelcomeGuest signals 'WhenAll' on completion
            // 5. BrowseInternet signals 'WhenAll' on completion
            // 6. 'WhenAll' resumes current routine from saved state.
            await Task.WhenAll(
                WelcomeGuest(),
                BrowseInternet());
            // In the cloud, this feature requires a persistent storage
            // that supports concurrency checks (like ETags for example).

            Console.WriteLine("I feel very productive today!");
        }

        protected virtual async Task WelcomeGuest()
        {
            Console.WriteLine("Welcome to the Coffee Shop!");
        }

        protected virtual async Task BrowseInternet()
        {
            Console.WriteLine("Aww.. hello kitties!");
        }
    }
}
