![D·ASYNC - Code First Distributed Applications](dasync-banner.png)

## What is D·ASYNC?

D·ASYNC is a framework that allows to model business processes and workflows in C# language itself, focus on the application logic and put infrastructure aside, keep Clean Architecture and delay design decisions, where compiled software runs in a distributed manner from day one. D·ASYNC extends the ideology of Distributed Domain-Driven Design by making the programming language and OOP patterns the first class-citizen (an ultimate façade) for expressing intentions and behavior of your business.

D·ASYNC technology deeply integrates into the .NET runtime, controls execution of `async` methods (which are compiled to Finite State Machines), saves their state in a persistent storage, uses Message-Oriented Architecture and Event-Driven Design to distribute load across available compute nodes.


## Few Programming Concepts

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

You can find more concepts in the [Syntax Mapping blog post](https://dasyncnet.wordpress.com/2018/05/04/dasync-syntax-mapping/).

## Examples

Keep in mind that D·ASYNC technology is in preview now.
* [Azure Functions Seamless Demo](https://dasyncnet.wordpress.com/2018/05/04/dasync-on-azure-functions/)
* [Feature Showdown Application](Examples/FeatureShowdown/Program.cs)

## More info

Don't hesitate to [contat about this project](https://dasyncnet.wordpress.com/contact/) if you have any questions or your want to contribute.
* [High-level Overview on TheNewStack.io](https://thenewstack.io/meet-d-async-a-framework-for-writing-distributed-cloud-native-applications/)
* [The story of development on DZone](https://dzone.com/articles/d-async-cloud-native-apps)
* [More details in blog posts](https://dasyncnet.wordpress.com/2018/05/04/what-is-dasync/)
* [NuGet packages](https://www.nuget.org/packages?q=dasync)
