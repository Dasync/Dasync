## What is D·ASYNC?

D·ASYNC (also D-ASYNC or DASYNC, where D stands for Distributed) is an ambitious framework for writing cloud-native distributed applications in C# language using just its syntax and paradigms of Object-Oriented Programming with the help of built-in support for Task Parallel Library (the async and await keywords).

1. The basics. And resiliency.
```csharp
// This is your 'service' or 'workflow'.
public class BaristaSimulationWorkflow
{
  // This is a 'routine' of a workflow.
  public virtual async Task Run()
  {
    // This will call a sub-routine and save the sate
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
public interface IOtherService
{
  Task DoAnotherThing();
}

public class MyService
{
  private IOtherService _otherService;

  // Another service/workflow can be consumed by
  // injecting as a dependency. All calls to that
  // service will be routed to that particular
  // deployment using its communication mechanism.
  // All replies will be routed back to this service.
  // This is where Dependency Injection meets Service
  // Discovery and Service Mesh.
  public MyService(IOtherService otherService)
  {
    _otherService = otherService;
  }
  
  public async Task DoThing()
  {
    // Simple call to another service may ensure
    // transactionality between two. That complexity
    // is hidden to help you focus on the business logic.
    await _otherService.DoAnotherThing();
  }
}
```

3. Scalability: Factory pattern and resource provisioning.
```csharp
public interface IMyWorkflow : IDisposable
{
  Task Run();
}

public interface IMyWorkflowFactory
{
  Task<IMyWorkflow> Create();
}

public class ControlService
{
  private IMyWorkflowFactory _factory;
  
  public ControlService(IMyWorkflowFactory factory)
  {
    _factory = factory;
  }
  
  public virtual async Task Run()
  {
    // Create an instance of a workflow, where 'under
    // the hood' it can provision necessary cloud
    // resources first. That is hidden behind the
    // factory abstraction, what allows to focus on
    // the business logic and put the infrastructure aside.
    using (var workflowInstance = await _factory.Create())
    {
      // This can be routed to a different cloud resource
      // or deployment what enables dynamic scalability.
      await workflowInstance.Run();
      // Calling IDisposable.Dispose() will de-provision
      // allocated resources.
    }
  }
}
```

4. Scalability: Parallel execution.
```csharp
public class MyWorkflow
{
  public virtual async Task Run()
  {
    // You can execute multiple routines in parallel
    // to increase performance by calling sub-routines.
    Task fooTask = RunFoo();
    Task barTask = RunBar();
    
    // Then just await all of them, as you would
    // normally do with TPL.
    await Task.WhenAll(fooTask, barTask);
    
    // And that will be translated into such series of steps:
    // 1. Save state of current routine;
    // 2. Schedule RunFoo
    // 3. Schedule RunBar
    // 4. RunFoo signals 'WhenAll' on completion
    // 5. RunBar signals 'WhenAll' on completion
    // 6. 'WhenAll' resumes current routine from saved state.
  }
}
```

5. Statefulness and instances.
```csharp
// This service has no private fields - it is stateless.
public class MyStatelessService
{
}

// This service has one or more private fields - it is stateful.
public class MyStatefulService
{
  private string _connectionString;
}

// Even though this service has a private field, it is
// stateless, because the field represents an injected
// dependency - something that can be re-constructed
// and does not need to be persisted.
public class MyStatelessService2
{
  private IOtherService _dependency;

  public MyStatelessService2(IOtherService dependency)
  {
    _dependency = dependency;
  }
}
 
// Most likely this factory service is a singleton,
// however it creates an instance of a service, which
// can be a multiton for example.
public interface IMyServiceFactory
{
  Task<IMyService> Create(string id);
}
```

6. Integration with other TPL functions.
```csharp
public class MyService
{
  public async Task EchoWithCheckpoint()
  {
    string input = Console.ReadLine();
    
    // Normally, 'Yield' instructs runtime to re-schedule
    // continuation of an async method, thus gives opportunity
    // for other work items on the thread pool to execute.
    // Similarly, DASYNC Execution Engine will save the sate
    // of the routine and will schedule its continuation,
    // possibly on a different node.
   
    await Task.Yield();
    
    // I.e. if the process terminates abruptly here (after
    // the Yield), the method will be re-tried from exact
    // point without calling Console.ReadLine again.
    
    Console.WriteLine(input);
  }

  public async Task PeriodicWakeUp()
  {
    for (var i = 0; i < 3; i++)
    {
      // The delay is translated by the DASYNC Execution
      // Engine to saving state of the routine and resuming
      // after given amount of time. This can be useful when
      // you do polling for example, but don't want the method
      // to be volatile (lose its execution context) and to
      // allocate resources in memory.
      await Task.Delay(20_000);
    }
  }
}
```

## More info
* [More details in blog posts](https://dasyncnet.wordpress.com/2018/05/04/what-is-dasync/)
* [Tech Preview Demo](https://dasyncnet.wordpress.com/2018/05/04/dasync-on-azure-functions/)
* [NuGet packages](https://www.nuget.org/packages?q=dasync)