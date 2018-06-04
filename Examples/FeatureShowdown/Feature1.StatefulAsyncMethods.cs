using System;
using System.Threading.Tasks;
using Dasync.Ioc.Ninject;
using Ninject;

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

    // This is a 'service', which defines a 'workflow' with async methods.
    public class BaristaWorker : IBaristaWorker
    {
        // An entry point to the workflow - a routine.
        public async Task PerformDuties()
        {
            var order = await TakeOrder();

            // !!! LOOK HERE !!!
            //
            // 1. Put a breakpoint to the line below.
            // 2. Run the demo, and when breakpoint hits, terminate the app.
            // 3. Re-start the app, choose the this feature to run again,
            //    and you'll see that previously 'crashed' routine will resume
            //    it's execution from exact point - prints your previous selection.
            //    You'll also see in the debugger the restored state of this method.

            Console.WriteLine($"You ordered {order.BeverageName}.");
        }

        // A sub-routine of the workkflow.
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
