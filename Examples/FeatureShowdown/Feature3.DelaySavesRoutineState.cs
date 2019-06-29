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
        public async Task LoungeAround()
        {
            // "I have nothing to do right now, let me solve this math problem.."
            var a = Environment.TickCount % 100;
            var b = (Environment.TickCount >> 4) % 100;
            Console.WriteLine($"I wonder what {a} times {b} is. Let me think..");

            // The delay is translated by the DASYNC Execution
            // Engine to saving state of the routine and resuming
            // after given amount of time. This can be useful when
            // you do polling for example, but don't want the method
            // to be volatile (lose its execution context) and/or to
            // allocate compute and memory resources.
            await Task.Delay(5_000);

            // !!! LOOK HERE !!!
            //
            // 1. Run the demo, and terminate the app during the delay.
            // 2. Simply re-start the app, choose the this feature to run again,
            //    and you'll see that previously 'crashed' routine will resume
            //    at exact point of time from exact point and.

            var c = a * b;
            Console.WriteLine($"I know, {a} times {b} is {c}!");
        }
    }
}
