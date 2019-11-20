using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures.Feature7
{
    /// <summary>
    /// Demonstrates reactive even-driven model where the Dependency Inversion
    /// principle and the Observer pattern are applied to the service level.
    /// </summary>
    public class Demo : IFeatureDemo
    {
        public string Name { get; } = "Events";

        public ICommunicationModel Model { get; } = CommunicationModelBuilder.Build(m => m
            .Service<CustomerManagementService>(s => { })
            .Service<NewsletterService>(s => { })
            .Service<RewardProgramService>(s => { }));

        public Dictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public async Task Run(IServiceProvider services)
        {
            var customerManagementService = services.GetService<ICustomerManagementService>();

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

        // This is a domain event (see Domain-Driven Design).
        event EventHandler<CustomerInfo> CustomerRegistered;
    }

    public class CustomerManagementService : ICustomerManagementService
    {
        public virtual async Task RegisterCustomer(CustomerInfo customerInfo)
        {
            await Console.Out.WriteLineAsync(
                $"Thank you for becoming the most valuable member with us, {customerInfo.FullName}!");

            // Raise the event, so any observing service can react to it.
            // In fact the event does not fire immediately, it gets scheduled
            // and committed in consistent manner as a part of the Unit of Work
            // represented by the state transition of this method.
            CustomerRegistered?.Invoke(this, customerInfo);
        }

        public virtual event EventHandler<CustomerInfo> CustomerRegistered;
    }

    public class NewsletterService
    {
        public NewsletterService(ICustomerManagementService customerManagementService)
        {
            // Subscribe to the event of another service of the distributed app.
            customerManagementService.CustomerRegistered += OnCustomerRegistered;
            // In a real cloud application, the pub-sub shoud be configured
            // externally in addition to this runtime declaration.
        }

        // This is a routine that does not return any result, but executed in
        // stateful and resilient manner as any other regular routine.
        protected virtual async void OnCustomerRegistered(object sender, CustomerInfo customerInfo)
        {
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
