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

        // !!! LOOK HERE !!!
        // IBaristaWorker is injected as a dependency on a distributed service.

        public CoffeeShopManager(IBaristaWorker baristaWorker)
        {
            _baristaWorker = baristaWorker;
        }

        public virtual async Task SolveComplaint()
        {
            Console.WriteLine("[manager] The visitor complained about a fly in the drink.");

            // !!! LOOK HERE !!!
            // Calling a routine on a distributed service will save the state
            // of current routine, because together they define a workflow.

            await _baristaWorker.PleaseMakeAnotherCoffee();
        }
    }

    public class BaristaWorker : IBaristaWorker
    {
        private readonly ICoffeeMachine _coffeeMachine;

        // !!! LOOK HERE !!!
        // ICoffeeMachine is injected as a simple dependency (not a distributed service).

        public BaristaWorker(ICoffeeMachine coffeeMachine)
        {
            _coffeeMachine = coffeeMachine;
        }

        public virtual async Task PleaseMakeAnotherCoffee()
        {
            Console.WriteLine("[barista] But that's a latte art! Fine..");

            // !!! LOOK HERE !!!
            // Calling a routine on a simple dependency will NOT save the state
            // of current routine, because it's not a part of a workflow.

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
