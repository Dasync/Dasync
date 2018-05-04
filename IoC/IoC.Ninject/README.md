This library is a part of the [D·ASYNC](https://github.com/Dasync) project.

Examples.

1. Let the D·ASYNC runtime know that your class is an application service:
```csharp
kernel.Bind<IMyAppService>().To<MyAppService>().AsService();
```

2. Let the D·ASYNC runtime know that your application depends on an external service, which must be resolved used the service discovery. The runtime will generate a proxy type on the `INotMyAppService` interface and will route all requests to the service using the discovered registration information.
```csharp
kernel.Bind<INotMyAppService>().ToExtrnalService();
```