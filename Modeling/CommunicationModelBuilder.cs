using System;

namespace Dasync.Modeling
{
    public class CommunicationModelBuilder
    {
        public CommunicationModelBuilder()
        {
            Model = new CommunicationModel();
#warning Auto-add system services? (Task.Yield, Task.WaitAll)
        }

        public static ICommunicationModel Build(Action<CommunicationModelBuilder> buildAction)
        {
            var builder = new CommunicationModelBuilder();
            buildAction(builder);
            return builder.Model;
        }

        public IMutableCommunicationModel Model { get; private set; }

        public ServiceDefinitionBuilder Service(string serviceName)
        {
            var existingServiceDefinition = Model.FindServiceByName(serviceName);
            if (existingServiceDefinition != null)
            {
                if (existingServiceDefinition.Type != ServiceType.Local)
                    throw new InvalidOperationException($"The service '{serviceName}' is already defined as '{existingServiceDefinition.Type}' and should not be re-defined as '{ServiceType.Local}'. Use a different builder method.");
                return new ServiceDefinitionBuilder((IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinition = new ServiceDefinition((CommunicationModel)Model);
            serviceDefinition.Type = ServiceType.Local;
            serviceDefinition.Name = serviceName;
            return new ServiceDefinitionBuilder(serviceDefinition);
        }

        public ServiceDefinitionBuilder Service(Type implementationType)
        {
            var existingServiceDefinition = Model.FindServiceByImplementation(implementationType);
            if (existingServiceDefinition != null)
            {
                if (existingServiceDefinition.Type != ServiceType.Local)
                    throw new InvalidOperationException($"The service '{existingServiceDefinition.Name}' is already defined as '{existingServiceDefinition.Type}' and should not be re-defined as '{ServiceType.Local}'. Use a different builder method.");
                return new ServiceDefinitionBuilder((IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinition = new ServiceDefinition((CommunicationModel)Model);
            serviceDefinition.Type = ServiceType.Local;
            serviceDefinition.Implementation = implementationType;

            var generatedServiceName = DefaultServiceNamer.GetServiceNameFromType(implementationType);
            if (Model.FindServiceByName(generatedServiceName) == null)
                serviceDefinition.Name = generatedServiceName;

            var defaultInterfaceType = DefaultServiceInterfaceFinder.FindDefaultInterface(implementationType);
            if (defaultInterfaceType != null && Model.FindServiceByInterface(defaultInterfaceType) == null)
                serviceDefinition.AddInterface(defaultInterfaceType);

            return new ServiceDefinitionBuilder(serviceDefinition);
        }

        public ServiceDefinitionBuilder Service(Type interfaceType, Type implementationType)
        {
            var existingServiceDefinition = Model.FindServiceByInterface(interfaceType);
            if (existingServiceDefinition != null)
            {
                if (existingServiceDefinition.Type != ServiceType.Local)
                    throw new InvalidOperationException($"The service '{existingServiceDefinition.Name}' is already defined as '{existingServiceDefinition.Type}' and should not be re-defined as '{ServiceType.Local}'. Use a different builder method.");
                return new ServiceDefinitionBuilder((IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinitionBuilder = Service(implementationType);
            serviceDefinitionBuilder.ServiceDefinition.AddInterface(interfaceType);
            return serviceDefinitionBuilder;
        }

        public ServiceDefinitionBuilder Service<TImplementation>() => Service(typeof(TImplementation));

        public ServiceDefinitionBuilder Service<TInterface, TImplementation>() =>
            Service(typeof(TInterface), typeof(TImplementation));

        public CommunicationModelBuilder Service(Type implementationType, Action<ServiceDefinitionBuilder> buildAction)
        {
            var serviceBuilder = Service(implementationType);
            buildAction(serviceBuilder);
            return this;
        }

        public CommunicationModelBuilder Service(Type interfaceType, Type implementationType, Action<ServiceDefinitionBuilder> buildAction)
        {
            var serviceBuilder = Service(interfaceType, implementationType);
            buildAction(serviceBuilder);
            return this;
        }

        public CommunicationModelBuilder Service<TImplementation>(Action<ServiceDefinitionBuilder> buildAction) =>
            Service(typeof(TImplementation), buildAction);

        public CommunicationModelBuilder Service<TInterface, TImplementation>(Action<ServiceDefinitionBuilder> buildAction) =>
            Service(typeof(TInterface), typeof(TImplementation), buildAction);

        public ExternalServiceDefinitionBuilder ExternalService(string serviceName)
        {
            var existingServiceDefinition = Model.FindServiceByName(serviceName);
            if (existingServiceDefinition != null)
            {
                if (existingServiceDefinition.Type != ServiceType.External)
                    throw new InvalidOperationException($"The service '{serviceName}' is already defined as '{existingServiceDefinition.Type}' and should not be re-defined as '{ServiceType.External}'. Use a different builder method.");
                return new ExternalServiceDefinitionBuilder((IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinition = new ServiceDefinition((CommunicationModel)Model);
            serviceDefinition.Type = ServiceType.External;
            serviceDefinition.Name = serviceName;
            return new ExternalServiceDefinitionBuilder(serviceDefinition);
        }

        public ExternalServiceDefinitionBuilder ExternalService(Type interfaceType)
        {
            var existingServiceDefinition = Model.FindServiceByInterface(interfaceType);
            if (existingServiceDefinition != null)
            {
                if (existingServiceDefinition.Type != ServiceType.External)
                    throw new InvalidOperationException($"The service '{existingServiceDefinition.Name}' is already defined as '{existingServiceDefinition.Type}' and should not be re-defined as '{ServiceType.External}'. Use a different builder method.");
                return new ExternalServiceDefinitionBuilder((IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinition = new ServiceDefinition((CommunicationModel)Model);
            serviceDefinition.Type = ServiceType.External;
            serviceDefinition.AddInterface(interfaceType);

            var generatedServiceName = DefaultServiceNamer.GetServiceNameFromType(interfaceType);
            if (Model.FindServiceByName(generatedServiceName) == null)
                serviceDefinition.Name = generatedServiceName;

            return new ExternalServiceDefinitionBuilder(serviceDefinition);
        }

        public CommunicationModelBuilder ExternalService(Type interfaceType, Action<ExternalServiceDefinitionBuilder> buildAction)
        {
            var serviceBuilder = ExternalService(interfaceType);
            buildAction(serviceBuilder);
            return this;
        }

        public ExternalServiceDefinitionBuilder ExternalService<TImplementation>() =>
            ExternalService(typeof(TImplementation));

        public CommunicationModelBuilder ExternalService<TImplementation>(Action<ExternalServiceDefinitionBuilder> buildAction) =>
            ExternalService(typeof(TImplementation), buildAction);

        public CommunicationModelBuilder EntityProjection(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"The type '{interfaceType}' must be an interface to qualify for an entity projection.");
            if (Model.FindEntityProjectionByIterfaceType(interfaceType) != null)
                return this;
            var definition = new EntityProjectionDefinition((CommunicationModel)Model, interfaceType);
            var builder = new EntityProjectionDefinitionBuilder(definition);
            return this;
        }

        public CommunicationModelBuilder EntityProjection<TInterface>() => EntityProjection(typeof(TInterface));
    }
}
