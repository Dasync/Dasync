![D·ASYNC - Code First Distributed Applications](dasync-banner.png)

## What is D·ASYNC?

You can think of D-ASYNC like EntityFramework but for inter-service communication and stateful workflows. Instead of focusing on domain entities and their mapping to data transfer objects, D-ASYNC framework targets domain commands, responses, queries, and events. The key feature of D-ASYNC framework is the ability to use C# language and .NET abstractions without a need to manually craft service clients or to define hierarchical state machines for persisted workflows.

D-ASYNC engine uses a combination of conventional object proxies and an innovative technology that controls the execution of `async` methods, which are compiled into state machines. The capability to save state of underlying state machines allows a Cloud/Edge service platform to be message-oriented and event-driven, what works much better in a distributed system than a traditional synchronous communication mechanism.

## How does it help?

For developers of service-oriented distributed systems, D-ASYNC framework can offer ~5x productivity boost as it hides the complexity of an 'infrastructure layer' and helps to focus more on the core business logic of an application.

You can find the grand vision in Part 4 of the **'Conquest of Distributed Systems'** story. Part 1 describes a typical problem, Part 2 shows one of the most common solutions, and Part 3 reveals the core of the technology.
- [Part 1: Business Workflows](https://medium.com/@sergiis/conquest-of-distributed-systems-part-1-business-workflows-fdda4b7b1c42)
- [Part 2: Orchestration with Actor Model](https://medium.com/@sergiis/actor-model-hidden-in-plain-sight-the-era-of-true-serverless-part-2-6f61470955e9)
- [Part 3: Actor Model Hidden in Plain Sight](https://medium.com/@sergiis/conquest-of-distributed-systems-part-3-actor-model-hidden-in-plain-sight-b06126a62ae)
- [Part 4: The Now and the Vision](https://medium.com/@sergiis/conquest-of-distributed-systems-part-4-the-now-and-the-vision-e844c9aee2c7)

## Few Programming Concepts
The following C# examples may look trivial and as for a single-process application, but D-ASYNC framework makes them run (without any modifications) in a resilient, persistent, scalable, and distributed manner.

1. The basics. And resiliency.
```csharp
// This is your 'service', part of a 'workflow'.
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
    // other actors, where async-await is the perfect
    // candidate for a Message-Oriented design.
        
    await Serve(cup);
  }
  
  // This is a 'sub-routine' of a workflow.
  protected virtual async Task<Order> TakeOrder();
  
  protected virtual async Task<Cup> MakeCoffee(Order order);
  
  protected virtual async Task Serve(Cup cup);
}
```

2. Inter-service communication, dependency injection, and transactionality.
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

  // Another service/workflow can be consumed by
  // injecting as a dependency. All calls to that
  // service will be routed to that particular
  // deployment using its communication mechanism.
  // All replies will be routed back to this service.
  // This is where Dependency Injection meets Service
  // Discovery and Service Mesh.
  public BaristaWorker(IPaymentTerminal paymentTerminal)
  {
    _paymentTerminal = paymentTerminal;
  }
  
  protected virtual async Task<Order> TakeOrder()
  {
    Order order = ...;
    CreditCard card = ...;
    // Simple call to another service may ensure
    // transactionality between two. That complexity
    // is hidden to help you focus on the business logic.
    await _paymentTerminal.Pay(order, card);
    // And again, state is saved here for resiliency.
  }
}
```

3. Scalability: Parallel execution.
```csharp
public class CoffeeMachine
{
  public virtual async Task PourCoffeeAndMilk(Cup cup)
  {
    // You can execute multiple routines in parallel
    // to 'horizontally scale out' the application.
    Task coffeeTask = PourCoffee(cup);
    Task milkTask = PourMilk(cup);
    
    // Then just await all of them, as you would
    // normally do with TPL.
    await Task.WhenAll(coffeeTask, milkTask);
    
    // And that will be translated into such series of steps:
    // 1. Save state of current routine;
    // 2. Schedule PourCoffee
    // 3. Schedule PourMilk
    // 4. PourCoffee signals 'WhenAll' on completion
    // 5. PourMilk signals 'WhenAll' on completion
    // 6. 'WhenAll' resumes current routine from saved state.
  }
}
```

4. Integration with other TPL functions.
```csharp
public class BaristaWorker
{
  protected virtual async Task<Order> TakeOrder()
  {
    var order = new Order();
    
    order.DrinkName = Console.ReadLine();
    
    // Normally, 'Yield' instructs runtime to re-schedule
    // continuation of an async method, thus gives opportunity
    // for other work items on the thread pool to execute.
    // Similarly, DASYNC Execution Engine will save the state
    // of the routine and will schedule its continuation,
    // possibly on a different node.
   
    await Task.Yield();
    
    // I.e. if the process terminates abruptly here (after
    // the Yield), the method will be re-tried from exact
    // point without calling Console.ReadLine again.
    
    order.PersonName = Console.ReadLine();
    
    // No need to call 'Yield' here, because this is the
    // end of the routine, which result will be committed
    // upon completion of the last step.
    
    return order;
  }

  public async Task ServeCustomers()
  {
    while (!TimeToGoHome)
    {
      if (!AnyNewCustomer)
      {
        // The delay is translated by the DASYNC Execution
        // Engine to saving state of the routine and resuming
        // after given amount of time. This can be useful when
        // you do polling for example, but don't want the method
        // to be volatile (lose its execution context) and/or to
        // allocate compute and memory resources.
        await Task.Delay(20_000);
      }
      else
      {
        ...
      }
    }
  }
}
```

5. Better Saga Pattern.
```csharp
public async Task PlaceOrder()
{
  // Generate unique ID which will be persisted in this routine.
  var transationId = Guid.NewGuid();

  var price = 10;
  var itemId = "Whole Coffee Beans 1lb";
  var quantity = 1;

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
  // The workflow becomes very clear due to absence of evident events.
}
```

6. Domain Events.
```csharp
public class CustomerManagementService : ICustomerManagementService
{
  // Implement the domain event simply as a C# event.
  public virtual event EventHandler<CustomerInfo> CustomerRegistered;

  public virtual async Task RegisterCustomer(CustomerInfo customerInfo)
  {
    // ... (register a new customer in the system here)

    // Then notify observers about successful registration.
    // The event does not fire immediately, but gets scheduled and
    // committed in consistent manner as a part of the unit of work
    // represented by the state transition of this routine.
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

  protected virtual async void OnCustomerRegistered(
    object sender, CustomerInfo customerInfo)
  {
    // This method is executed as a routine in stateful and resilient
    // manner as any other regular routine, but does not return any result.
  }
}
```

You can find more concepts in the [Syntax Mapping blog post](https://dasyncnet.wordpress.com/2018/05/04/dasync-syntax-mapping/).

## Examples

Keep in mind that D·ASYNC technology is in preview now.
* [Azure Functions Seamless Demo](https://dasyncnet.wordpress.com/2018/05/04/dasync-on-azure-functions/)
* [Feature Showdown Application](Examples/FeatureShowdown)

## State of the project

The D-ASYNC project consists of two logical parts: the platform-independent execution engine, and a platform. The same way EntityFramework has its core and numerous providers.

The D-ASYNC execution engine is in pre-release state. The first platform is under active development to serve as a reference for other platforms. And there are many more features coming after the first release.

## More info

Don't hesitate to [contact about this project](https://dasyncnet.wordpress.com/contact/) if you have any questions or your want to contribute.
* [High-level Overview on TheNewStack.io](https://thenewstack.io/meet-d-async-a-framework-for-writing-distributed-cloud-native-applications/)
* [The story of development on DZone](https://dzone.com/articles/d-async-cloud-native-apps)
* [More details in blog posts](https://dasyncnet.wordpress.com/2018/05/04/what-is-dasync/)
* [NuGet packages](https://www.nuget.org/packages?q=dasync)

## Licencing

The D-ASYNC project is open-source and absolutely free to use. It is licensed under modified conditions of the CDDL-1.0 license which don't allow to sell this software as a service - serves a similar purpose as recently introduced Confluent's 'Confluent Community License' and MongoDB's 'Server Side Public License'.
