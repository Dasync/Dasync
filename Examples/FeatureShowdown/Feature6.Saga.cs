using Dasync.Ioc.Ninject;
using Ninject;
using System;
using System.Threading.Tasks;

namespace DasyncFeatures.Feature6
{
    /// <summary>
    /// Demonstrates an easy way to implement a Saga Pattern, where the
    /// workflow is absolutely clear due to absence of evident events.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Saga";

        public IKernel AppKernel { get; } = new StandardKernel();

        public Demo()
        {
            AppKernel.Bind<IOrderPlacement>().To<OrderPlacement>().AsService();
            AppKernel.Bind<IPaymentProcessor>().To<PaymentProcessor>().AsService();
            AppKernel.Bind<IWarehouse>().To<Warehouse>().AsService();
        }

        public async Task Run()
        {
            var orderPlacement = AppKernel.Get<IOrderPlacement>();
            await orderPlacement.PlaceOrder();
        }
    }

    public interface IOrderPlacement
    {
        Task PlaceOrder();
    }

    public interface IPaymentProcessor
    {
        Task Credit(Guid transationId, int amount);
        Task Debit(Guid transationId, int amount);
    }

    public interface IWarehouse
    {
        Task ReserveItem(Guid reservationId, string itemId, int quantity);
    }

    public class OrderPlacement : IOrderPlacement
    {
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly IWarehouse _warehouse;

        public OrderPlacement(IPaymentProcessor paymentProcessor, IWarehouse warehouse)
        {
            _paymentProcessor = paymentProcessor;
            _warehouse = warehouse;
        }

        public async Task PlaceOrder()
        {
            // Generate unique ID which will be persisted in this routine.
            var transationId = Guid.NewGuid();

            var price = 10;
            var itemId = "Whole Coffee Beans 1lb";
            var quantity = 1;

            // !!! LOOK HERE !!!
            // This is implementation of the Saga Pattern.
            // Remember, any step will be re-tried if the process fails abruptly.

            // 1. First, make sure that payment can be made.
            // This is a call to a service #1.
            await _paymentProcessor.Credit(transationId, price);
            try
            {
                // 2. Then, reserve the item being purchased.
                // This is a call to a service #2.
                await _warehouse.ReserveItem(transationId, itemId, quantity);
                // 3. Well, they are out of stock.
                // The OutOfStockException is thrown.
            }
            catch
            {
                // 4. Refund the cost of an item.
                // Perform a compensating action on service #1.
                await _paymentProcessor.Debit(transationId, price);
            }

            // All in all, this async method (a routine) acts as an orchestrator.
            // Invoking and subscribing to continuations of async methods of two
            // services can be viewed as sending commands and listening to events. 
        }
    }

    public class PaymentProcessor : IPaymentProcessor
    {
        public async Task Credit(Guid transationId, int amount)
        {
            // The 'transationId' can be used to make this
            // action idempotent and avoid double charge.
        }

        public async Task Debit(Guid transationId, int amount)
        {
            // The 'transationId' can be used to make this
            // action idempotent and avoid double refund.
        }
    }

    public class Warehouse : IWarehouse
    {
        public async Task ReserveItem(Guid reservationId, string itemId, int quantity)
        {
            // The 'reservationId' can be used to make this
            // action idempotent and avoid double reservation.

            throw new OutOfStockException();
        }
    }

    public class OutOfStockException : Exception { }
}
