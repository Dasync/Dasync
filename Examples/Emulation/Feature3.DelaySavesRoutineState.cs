using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures.Feature3
{
    /// <summary>
    /// Demonstrates an integration with TPL 'Delay' function, that serves
    /// as a checkpoint with delaying delivering a routine transition event.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Delay and Save State with Task.Delay";

        public ICommunicationModel Model { get; } = CommunicationModelBuilder.Build(m => m.Service<BaristaWorker>(s => { }));

        public Dictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public async Task Run(IServiceProvider services)
        {
            var baristaWorker = services.GetService<IBaristaWorker>();
            await baristaWorker.LoungeAround();
        }
    }

    public interface IBaristaWorker
    {
        Task LoungeAround();
    }

    public class BaristaWorker : IBaristaWorker
    {
        public virtual async Task LoungeAround()
        {
            var a = Environment.TickCount % 100;
            var b = (Environment.TickCount >> 4) % 100;
            Console.WriteLine($"I wonder what {a} times {b} is. Let me think..");

            // The delay is translated by the D-ASYNC Execution
            // Engine to saving state of the method and resuming
            // after given amount of time. This can be useful when
            // you do polling for example, but don't want the method
            // to be volatile (lose its execution context) and/or to
            // allocate compute and memory resources.
            await Task.Delay(5_000);
            // For example, when a message queue is used, a message
            // with delay of 5 seconds will be placed on a queue
            // to resume execution of this method from this point.

            var c = a * b;
            Console.WriteLine($"I know, {a} times {b} is {c}!");
        }
    }
}
