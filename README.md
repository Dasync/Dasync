[![Build Status](https://github.com/Dasync/Dasync/workflows/keeps/badge.svg?event=push&branch=master)](https://github.com/Dasync/Dasync/actions?workflow=keeps)
[![Follow on Twitter](https://img.shields.io/twitter/follow/kind_serge.svg?style=social&logo=twitter)](https://twitter.com/intent/follow?screen_name=kind_serge)
[![Stories on Medium](https://img.shields.io/static/v1?label=&message=Stories&color=grey&logo=medium&logoColor=white&labelColor=black)](https://medium.com/@sergiis)
[![D-ASYNC | Cloud-Native Apps Without Frameworks](dasync-banner.png)](http://dasync.io)

## What is D·ASYNC?

What if you don't need to write API controllers? How about you don't need to write message and event handlers? Imagine if you can create services and entirely skip the non-functional boilerplate code for inter-service communication and workflows.

The mission of D-ASYNC is to convert programming-language abstractions into communication primitives of modern cloud applications based on distributed services. The ability to use the language itself helps to focus on the core value of your application, making it easy to write, read, evolve, and maintain.


## Basic Programming Concepts in C#
The following examples may look trivial and as for a single-process application, but D-ASYNC platform makes them run (without any modifications) in a resilient, persistent, scalable, and distributed manner. The new service-oriented syntax will be introduced with the [CloudSharp](https://github.com/Dasync/CloudSharp) project.

1. Inter-Service Communication.
```csharp
// Declaration of the interface of another service
// that might be deployed in a different environment.
public interface IPaymentTerminal
{
  Task Pay(Order order, CreditCard card);
}

public class BaristaWorker
{
  private IPaymentTerminal _paymentTerminal;

  // Another service can be used with the dependency
  // injection. All calls to that service will use
  // its communication mechanism. All replies will
  // be routed back to this service.
  public BaristaWorker(IPaymentTerminal paymentTerminal)
  {
    _paymentTerminal = paymentTerminal;
  }
  
  protected virtual async Task<Order> TakeOrder()
  {
    Order order = ...;
    CreditCard card = ...;
    
    // Simple call to another service may ensure
    // consistency between two. That complexity is
    // hidden to help you focus on the business logic.
    await _paymentTerminal.Pay(order, card);
    
    // Unlike any RPC, the state of execution is saved,
    // and restored when the service replies. If the call
    // fails, it is automatically retried and an exception
    // is never seen in this method.
    
    // There is no need to create API controllers and service
    // clients, and no need to worry about asynchronous APIs.
  }
}
```

2. Service Events and Reactive Programming.
```csharp
public class CustomerManagementService : ICustomerManagementService
{
  // Implement the domain event simply as a C# event.
  public virtual event EventHandler<CustomerInfo> CustomerRegistered;

  public virtual async Task RegisterCustomer(CustomerInfo customerInfo)
  {
    // ... (register a new customer in the system here)

    // Then notify observers about successful registration.
    // The event may not fire immediately, but will get scheduled
    // when this method exits to guarantee consistency.
    CustomerRegistered?.Invoke(this, customerInfo);
  }
}

public class RewardProgramService : IRewardProgramService
{
  public RewardProgramService(
    ICustomerManagementService customerService)
  {
    // Subscribe to the domain event.
    customerService.CustomerRegistered += OnCustomerRegistered;
  }

  protected virtual void OnCustomerRegistered(
    object sender, CustomerInfo customerInfo)
  {
    // This method is executed in a resilient manner
    // and can be a workflow (see next example).
  }
}
```

3. Stateful Workflows.
```csharp
// This is a service with a workflow.
public class BaristaWorker
{
  // This is a 'routine' of a workflow.
  public virtual async Task PerformDuties()
  {
    // This will call a sub-routine and save the state
    // of the current one.
    var order = await TakeOrder();
    
    // If the process terminates abruptly here, after restart
    // the routine continue at exact point without executing
    // previous steps. Any async method is compiled into a
    // state machine, so it's possible to save and restore
    // its state and context.
    
    var cup = await MakeCoffee(order);
    
    // Essentially this is an Actor Model of a scalable
    // distributed system. A routine maps to an actor,
    // because an async method compiles into a state
    // machine (which has its state), and a routine can
    // call sub-routines - same as an actor can invoke
    // other actors.
        
    await Serve(cup);
    
    // Workflows are not limited to one service and
    // its methods, and can span across many services
    // (as shown in the very first example and below).
    
    // Varies degrees of consistency can be guaranteed
    // in case of failures, and that settings does not
    // change the business logic.
  }
  
  // This is a 'sub-routine' of a workflow.
  protected virtual async Task<Order> TakeOrder();
  
  protected virtual async Task<Cup> MakeCoffee(Order order);
  
  protected virtual async Task Serve(Cup cup);
}
```

4. Saga Pattern.
```csharp
// This method represents a workflow with
// application-specific error-handling.
public async Task PlaceOrder()
{
  // Hard-coded sample input.
  var price = 10;
  var itemId = "Whole Coffee Beans 1lb";
  var quantity = 1;

  // Generate unique ID which will be persisted in this routine.
  var transationId = Guid.NewGuid();

  // 1. First, make sure that payment can be made.
  // This is a call to a service #1.
  await _paymentProcessor.Credit(transationId, price);
  try
  {
    // 2. Then, reserve the item being purchased.
    // This is a call to a service #2.
    await _warehouse.ReserveItem(transationId, itemId, quantity);
  }
  catch (OutOfStockException)
  {
    // 3. If they are out of stock, refund the cost of an item.
    // Perform a compensating action on service #1.
    await _paymentProcessor.Debit(transationId, price);
  }

  // This method acts as an orchestrator and represents clear
  // business logic of placing an order without a need to
  // decompose it into dozens of message classes and their
  // respective handlers.
}
```

5. Parallel execution.
```csharp
public class CoffeeMachine
{
  public virtual async Task PourCoffeeAndMilk(Cup cup)
  {
    // You can execute multiple methods in parallel
    // to 'horizontally scale out' the application.
    Task coffeeTask = PourCoffee(cup);
    Task milkTask = PourMilk(cup);
    
    // Then just await all of them, as you would
    // normally do with TPL.
    await Task.WhenAll(coffeeTask, milkTask);
    
    // This is translated into a such series of steps:
    // 1. Save state of current method;
    // 2. Schedule PourCoffee
    // 3. Schedule PourMilk
    // 4. PourCoffee signals 'WhenAll' on completion
    // 5. PourMilk signals 'WhenAll' on completion
    // 6. 'WhenAll' resumes the current method from saved state.
    
    // The built-in concurrency control removes the
    // complexity of writing a synchronization logic
    // of distributed workflows.
  }
}
```


## Demo

The technology and the full solution is currently matures with early adopters in a closed environment.

The current beta version is available to the public as demonstrated in the [AspNetCoreDocker](examples/AspNetCoreDocker) example application.

You may [sign-up for the early experience](https://www.dasync.io/#comp-jyrluu0u) with a final managed solution that is under development.


## More Info

- [Part 1: Business Workflows](https://medium.com/@sergiis/conquest-of-distributed-systems-part-1-business-workflows-fdda4b7b1c42)
- [Part 2: Orchestration with Actor Model](https://medium.com/@sergiis/actor-model-hidden-in-plain-sight-the-era-of-true-serverless-part-2-6f61470955e9)
- [Part 3: Actor Model Hidden in Plain Sight](https://medium.com/@sergiis/conquest-of-distributed-systems-part-3-actor-model-hidden-in-plain-sight-b06126a62ae)
- [Part 4: The Now and the Vision](https://medium.com/@sergiis/conquest-of-distributed-systems-part-4-the-now-and-the-vision-e844c9aee2c7)

* [High-level Overview on TheNewStack.io](https://thenewstack.io/meet-d-async-a-framework-for-writing-distributed-cloud-native-applications/)
* [The story of development on DZone](https://dzone.com/articles/d-async-cloud-native-apps)
* [Cloud# - Service-Oriented Language Extensions for C#](https://github.com/Dasync/CloudSharp)
* [NuGet packages](https://www.nuget.org/packages?q=dasync)


## Why D·ASYNC?

We believe that every developer deserves the right of creating microservices without using any framework 🤍