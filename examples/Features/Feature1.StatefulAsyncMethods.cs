using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures.Feature1
{
    /// <summary>
    /// Shows the basic concept of resillient workflows governed by an
    /// event-driven design. An async method is compiled into a state
    /// machine, which state can be saved and restored on demand. The
    /// await keyword acts as subsription to and event - a completion
    /// of a method being called.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Stateful Async Methods";

        public ICommunicationModel Model { get; } = CommunicationModelBuilder.Build(m => m.Service<BaristaWorker>(s => { }));

        public Dictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public async Task Run(IServiceProvider services)
        {
            var baristaWorker = services.GetService<IBaristaWorker>();
            await baristaWorker.PerformDuties();
        }
    }

    // Defines a service contract.
    public interface IBaristaWorker
    {
        Task PerformDuties();
    }

    // This is a 'service', which defines a 'workflow' with async methods.
    public class BaristaWorker : IBaristaWorker
    {
        // The entry point to a workflow.
        public virtual async Task PerformDuties()
        {
            // Step 1 of the workflow: call a sub-routine.
            // The state of the execution is saved here.
            // The call of 'TakeOrder' can use HTTP, a message
            // queue, or any other mechanism configured.
            var order = await TakeOrder();
            // Step 2 of the workflow: react to the result
            // of the sub-routine and continue form saved state.

            Console.WriteLine($"You ordered {order.BeverageName}.");
        }

        // A sub-routine of the workflow.
        protected virtual async Task<Order> TakeOrder()
        {
            Console.Write("What beverage would you like? ");
            return new Order
            {
                BeverageName = Console.ReadLine()
            };
        }
    }

    public struct Order
    {
        public string BeverageName { get; set; }
    }
}
