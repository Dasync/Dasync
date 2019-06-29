using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures.Feature2
{
    /// <summary>
    /// Demonstrates an integration with TPL 'Yield' function, that serves
    /// as a manual checkpoint that saves the state of a routine.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Manual Checkpoint with Task.Yield";

        public ICommunicationModel Model { get; } = CommunicationModelBuilder.Build(m => m.Service<BaristaWorker>(s => { }));

        public Dictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public async Task Run(IServiceProvider services)
        {
            var baristaWorker = services.GetService<IBaristaWorker>();
            await baristaWorker.TakeOrder();
        }
    }

    public interface IBaristaWorker
    {
        Task TakeOrder();
    }

    public class BaristaWorker : IBaristaWorker
    {
        public async Task TakeOrder()
        {
            Console.Write("What beverage would you like? ");
            var beverageName = Console.ReadLine();

            // Normally, 'Yield' instructs runtime to re-schedule
            // continuation of an async method, thus gives opportunity
            // for other work items on the thread pool to execute.
            // Similarly, DASYNC Execution Engine will save the sate
            // of the routine and will schedule its continuation.
            await Task.Yield();

            // !!! LOOK HERE !!!
            //
            // 1. Run the demo, and when it asks about your name, terminate the app.
            // 2. Simply re-start the app, choose the this feature to run again,
            //    and you'll see that previously 'crashed' routine will resume
            //    it's execution from exact point and ask about your name again
            //    without asking for beverage.
            //    You won't see an immediate update in the console due to concurrent
            //    use by multiple threads.

            Console.Write("And your name is..? ");
            var personName = Console.ReadLine();

            await Task.Yield();

            Console.WriteLine($"{beverageName} for {personName} please!");
        }
    }
}
