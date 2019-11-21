using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures.Feature5
{
    /// <summary>
    /// Shows difference between injected dependencies, where distributed
    /// services guide the application behavior (the workflow).
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Service Dependency Injection";

        public ICommunicationModel Model { get; } = CommunicationModelBuilder.Build(m => m
            .Service<CoffeeShopManager>(s => { })
            .Service<BaristaWorker>(s => { }));

        public Dictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>
        {
            [typeof(ICoffeeMachine)] = typeof(CoffeeMachine)
        };

        public async Task Run(IServiceProvider services)
        {
            var manager = services.GetService<ICoffeeShopManager>();
            await manager.SolveComplaint();
        }
    }

    public interface ICoffeeShopManager
    {
        Task SolveComplaint();
    }

    public interface IBaristaWorker
    {
        Task PleaseMakeAnotherCoffee();
    }

    public interface ICoffeeMachine
    {
        Task BrewCoffee();
    }

    public class CoffeeShopManager : ICoffeeShopManager
    {
        private readonly IBaristaWorker _baristaWorker;

        // IBaristaWorker is injected as a dependency.
        // These two services can be co-located in the same process, or they
        // can have different deployment and share the interface only.
        public CoffeeShopManager(IBaristaWorker baristaWorker)
        {
            _baristaWorker = baristaWorker;
        }

        public virtual async Task SolveComplaint()
        {
            Console.WriteLine("[manager] The visitor complained about a fly in the drink.");

            // Similaril to example #1, this is a call to a sub-routine,
            // but of a different service with a different deployment (usually).
            await _baristaWorker.PleaseMakeAnotherCoffee();
        }
    }

    public class BaristaWorker : IBaristaWorker
    {
        private readonly ICoffeeMachine _coffeeMachine;

        // ICoffeeMachine is not declared as a service in the communication model,
        // thus injected as a regular dependency (not a service proxy).
        public BaristaWorker(ICoffeeMachine coffeeMachine)
        {
            _coffeeMachine = coffeeMachine;
        }

        public virtual async Task PleaseMakeAnotherCoffee()
        {
            Console.WriteLine("[barista] But that's a latte art! Fine..");

            // This call is not to a sub-routine since ICoffeeMachine is not a service.
            await _coffeeMachine.BrewCoffee();
        }
    }

    public class CoffeeMachine : ICoffeeMachine
    {
        public virtual async Task BrewCoffee()
        {
            Console.WriteLine("[coffee machine] 'Pshhhhh'");
        }
    }
}
