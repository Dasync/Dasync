using System;
using System.Threading.Tasks;
using Dasync.Ioc.Ninject;
using Ninject;

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

        public IKernel AppKernel { get; } = new StandardKernel();

        public Demo()
        {
            AppKernel.Bind<IBaristaWorker>().To<BaristaWorker>().AsService();
        }

        public async Task Run()
        {
            var baristaWorker = AppKernel.Get<IBaristaWorker>();
            await baristaWorker.PerformDuties();
        }
    }

    public interface IBaristaWorker
    {
        Task PerformDuties();
    }

    public class BaristaWorker : IBaristaWorker
    {
        public async Task PerformDuties()
        {
            // WhenAll is translated into such series of steps:
            // 1. Save state of current routine;
            // 2. Schedule WelcomeGuest
            // 3. Schedule BrowseInternet
            // 4. WelcomeGuest signals 'WhenAll' on completion
            // 5. BrowseInternet signals 'WhenAll' on completion
            // 6. 'WhenAll' resumes current routine from saved state.
            await Task.WhenAll(
                WelcomeGuest(),
                BrowseInternet());

            // !!! LOOK HERE !!!
            //
            // You can put breakpoints both to WelcomeGuest and BrowseInternet
            // methods, and terminate the application when second breakpoint
            // hits. Then, after re-starting the demo app with this feature run,
            // the DASYNC runtime will pick it up from where it left.

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
