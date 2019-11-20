This libarary is a part of the [Dasync](https://github.com/tyrotoxin/Dasync) project.

Examples.

1. Create a proxy on a class:
```csharp
// Build proxy type
var proxyTypeBuilder = new ProxyTypeBuilder();
var proxyType = proxyTypeBuilder.Build(typeof(MyService));

// Instead of Activator, you can use an IoC container
// to properly inject dependencies.
var proxy = (IProxy)Activator.CreateInstance(proxyType);
// Set an executor that simply invokes the target method.
proxy.Executor = new PassThroughExecutor();

// Cast proxy instance to the service type.
var service = (MyService)proxy;
// Invoke the method via the PassThroughExecutor.
var result = service.SayHiTo("World!").Result;
Console.WriteLine(result);

// ...

public class MyService
{
    // The method must be virtual, so a proxy type can override
    // it and redirect to an IProxyMethodExecutor.
    public virtual Task<string> SayHiTo(string name)
    {
        return Task.FromResult("Hi, " + name);
    }
}

public class PassThroughExecutor : IProxyMethodExecutor
{
    public Task Execute<TParameters>(IProxy proxy,
        MethodInfo methodInfo, ref TParameters parameters)
        where TParameters : IValueContainer
    {
        // Create a method invoker.
        var invokerFactory = new MethodInvokerFactory();
        var invoker = invokerFactory.Create(methodInfo);
        // Invoke the target method. This time it won't use the
        // virtual method table and will call the target method
        // directly, thus not resulting in a recursive call.
        return invoker.Invoke(proxy, parameters);
    }
}
```

2. Create a proxy on an interface:
```csharp
// Build proxy type
var proxyTypeBuilder = new ProxyTypeBuilder();
var proxyType = proxyTypeBuilder.Build(typeof(IMyService));

// Create an instance of a dynamically-built type that
// implements the IMyService interface.
var proxy = (IProxy)Activator.CreateInstance(proxyType);
// Set an executor that simply invokes the target method.
proxy.Executor = new PassThroughExecutor();
// You can set any data here, just set the type of a class
// that implements IMyService for simplicity.
proxy.Context = typeof(MyService);

// Cast proxy instance to the service type.
var service = (IMyService)proxy;
// Invoke the method via the PassThroughExecutor.
var result = service.SayHiTo("World!").Result;
Console.WriteLine(result);

// ...

public interface IMyService
{
    Task<string> SayHiTo(string name);
}

public class MyService : IMyService
{
    public Task<string> SayHiTo(string name)
    {
        return Task.FromResult("Hi, " + name);
    }
}

public class PassThroughExecutor : IProxyMethodExecutor
{
    public Task Execute<TParameters>(IProxy proxy,
        MethodInfo methodInfo, ref TParameters parameters)
        where TParameters : IValueContainer
    {
        // Instead of Activator, you can use an IoC container
        // to properly inject dependencies.
        var classType = (Type)proxy.Context;
        var targetInstance = Activator.CreateInstance(classType);

        // Resolve the target method to invoke - we can't execute
        // interface methods because they have no implementation.
        // Here you should use proper mapping of an interface method
        // to the class method. Getting a method by name is shown
        // for simplicity.
        var targetMethod = classType.GetMethod(methodInfo.Name);

        // Create a method invoker.
        var invokerFactory = new MethodInvokerFactory();
        var invoker = invokerFactory.Create(targetMethod);
        // Invoke the target method on actual class that
        // implements the interface.
        return invoker.Invoke(targetInstance, parameters);
    }
}
```

3. Invoking a method from a serialized input:
```csharp
var targetMethod = typeof(MyService).GetMethod(nameof(MyService.SayHiTo));
var invokerFactory = new MethodInvokerFactory();
var invoker = invokerFactory.Create(targetMethod);
var parametersContainer = invoker.CreateParametersContainer();
var json = @"{ ""name"": ""World!"" }";
NewtonSoft.Json.JsonConvert.PopulateObject(json, parametersContainer);
var targetInstance = new MyService();
var resultTask = (Task<string>)invoker.Invoke(targetInstance, parametersContainer);
Console.WriteLine(resultTask.Result);

// ...

public class MyService
{
    public Task<string> SayHiTo(string name)
    {
        return Task.FromResult("Hi, " + name);
    }
}
```