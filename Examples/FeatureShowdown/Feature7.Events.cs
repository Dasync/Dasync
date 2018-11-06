using System;
using System.Threading.Tasks;
using Dasync.Ioc.Ninject;
using Ninject;

namespace DasyncFeatures.Feature7
{
    /// <summary>
    /// Demonstrates reactive even-driven model where the Dependency Inversion
    /// principle and the Observer pattern are applied to the service level.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Events";

        public IKernel AppKernel { get; } = new StandardKernel();

        public Demo()
        {
            AppKernel.Bind<ICustomerManagementService>().To<CustomerManagementService>().AsService();
            AppKernel.Bind<NewsletterService>().ToSelf().AsService();
            AppKernel.Bind<RewardProgramService>().ToSelf().AsService();
        }

        public async Task Run()
        {
            var customerManagementService = AppKernel.Get<ICustomerManagementService>();
            // Initialize all services on startup to subscribe to events.
            AppKernel.Get<NewsletterService>();
            AppKernel.Get<RewardProgramService>();

            var customerInfo = new CustomerInfo
            {
                FullName = "Serge Semenov",
                EmailAddress = "serge@dasync.io"
            };

            await customerManagementService.RegisterCustomer(customerInfo);

            // Give some time for the event to be observed.
            await Task.Delay(100);
        }
    }

    public struct CustomerInfo
    {
        public string FullName;
        public string EmailAddress;
    }

    public interface ICustomerManagementService
    {
        Task RegisterCustomer(CustomerInfo customerInfo);

        // !!! LOOK HERE !!!
        // This is a true event of a distributed application.
        event EventHandler<CustomerInfo> CustomerRegistered;
    }

    public class CustomerManagementService : ICustomerManagementService
    {
        public virtual async Task RegisterCustomer(CustomerInfo customerInfo)
        {
            await Console.Out.WriteLineAsync(
                $"Thank you for becoming the most valuable member with us, {customerInfo.FullName}!");

            // !!! LOOK HERE !!!
            // Raise the event, so any observing service can react to it.
            // In fact the event does not fire immediately, it gets scheduled
            // and committed in consistent manner as a part of the unit of work
            // represented by the state transition of this routine.
            CustomerRegistered?.Invoke(this, customerInfo);
        }

        public virtual event EventHandler<CustomerInfo> CustomerRegistered;
    }

    public class NewsletterService
    {
        public NewsletterService(ICustomerManagementService customerManagementService)
        {
            // !!! LOOK HERE !!!
            // Subscribe to the event of another service of the distributed app.
            customerManagementService.CustomerRegistered += OnCustomerRegistered;
        }

        // !!! LOOK HERE !!!
        // This is a routine that does not return any result, but executed in
        // stateful and resilient manner as any other regular routine.
        protected virtual async void OnCustomerRegistered(object sender, CustomerInfo customerInfo)
        {
            // FYI: the 'sender' can be casted to 'ICustomerManagementService'.

            await Console.Out.WriteLineAsync(
                $"We will send news every minute to '{customerInfo.EmailAddress}' with no option to opt out. ");
        }
    }

    public class RewardProgramService
    {
        public RewardProgramService(ICustomerManagementService customerManagementService)
        {
            customerManagementService.CustomerRegistered += OnCustomerRegistered;
        }

        protected virtual async void OnCustomerRegistered(object sender, CustomerInfo customerInfo)
        {
            await Console.Out.WriteLineAsync(
                $"We added -1,000,000 points to your reward balance as a sign up punishment!");
        }
    }
}
