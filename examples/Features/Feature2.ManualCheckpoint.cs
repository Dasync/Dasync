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
        public virtual async Task TakeOrder()
        {
            Console.Write("What beverage would you like? ");
            var beverageName = Console.ReadLine();

            // Normally, 'Yield' instructs runtime to re-schedule
            // continuation of an async method, thus gives opportunity
            // for other work items on the thread pool to execute.
            // Similarly, D-ASYNC engine will save the sate
            // of the routine and will schedule its continuation.
            await Task.Yield();
            // This will save the state of this method (the 'beverageName'
            // variable in this case), so in case when something fails
            // down the line the method will be re-tried from this point
            // instead of starting from the very beginning.
            // The state can be saved in a cloud tabular or blob/file storage.

            Console.Write("And your name is..? ");
            var personName = Console.ReadLine();

            await Task.Yield();

            Console.WriteLine($"{beverageName} for {personName} please!");
        }
    }
}
