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
            ModelRefiner.Refine(builder);
            var model = builder.Model;
            //ValidateModel(model);
            return model;
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

            return ServiceDefinitionBuilder.CreateByImplementationType(implementationType, serviceDefinition);
        }

        public ServiceDefinitionBuilder Service(Type interfaceType, Type implementationType)
        {
            var existingServiceDefinition = Model.FindServiceByInterface(interfaceType);
            if (existingServiceDefinition != null)
            {
                if (existingServiceDefinition.Type != ServiceType.Local)
                    throw new InvalidOperationException($"The service '{existingServiceDefinition.Name}' is already defined as '{existingServiceDefinition.Type}' and should not be re-defined as '{ServiceType.Local}'. Use a different builder method.");
                return ServiceDefinitionBuilder.CreateByImplementationType(implementationType, (IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinitionBuilder = Service(implementationType);
            serviceDefinitionBuilder.ServiceDefinition.AddInterface(interfaceType);
            return serviceDefinitionBuilder;
        }

        public ServiceDefinitionBuilder<TImplementation> Service<TImplementation>() =>
            (ServiceDefinitionBuilder<TImplementation>)Service(typeof(TImplementation));

        public ServiceDefinitionBuilder<TImplementation> Service<TInterface, TImplementation>() =>
            (ServiceDefinitionBuilder<TImplementation>)Service(typeof(TInterface), typeof(TImplementation));

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

        public CommunicationModelBuilder Service<TImplementation>(Action<ServiceDefinitionBuilder<TImplementation>> buildAction)
        {
            var serviceBuilder = Service<TImplementation>();
            buildAction(serviceBuilder);
            return this;
        }

        public CommunicationModelBuilder Service<TInterface, TImplementation>(Action<ServiceDefinitionBuilder<TImplementation>> buildAction)
        {
            var serviceBuilder = Service<TInterface, TImplementation>();
            buildAction(serviceBuilder);
            return this;
        }

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
                return ExternalServiceDefinitionBuilder.CreateByInterfaceType(interfaceType, (IMutableServiceDefinition)existingServiceDefinition);
            }

            var serviceDefinition = new ServiceDefinition((CommunicationModel)Model);
            serviceDefinition.Type = ServiceType.External;
            serviceDefinition.AddInterface(interfaceType);

            var generatedServiceName = DefaultServiceNamer.GetServiceNameFromType(interfaceType);
            if (Model.FindServiceByName(generatedServiceName) == null)
                serviceDefinition.Name = generatedServiceName;

            return ExternalServiceDefinitionBuilder.CreateByInterfaceType(interfaceType, serviceDefinition);
        }

        public CommunicationModelBuilder ExternalService(Type interfaceType, Action<ExternalServiceDefinitionBuilder> buildAction)
        {
            var serviceBuilder = ExternalService(interfaceType);
            buildAction(serviceBuilder);
            return this;
        }

        public ExternalServiceDefinitionBuilder<TInterface> ExternalService<TInterface>() =>
            (ExternalServiceDefinitionBuilder<TInterface>)ExternalService(typeof(TInterface));

        public CommunicationModelBuilder ExternalService<TInterface>(Action<ExternalServiceDefinitionBuilder<TInterface>> buildAction)
        {
            var builder = ExternalService<TInterface>();
            buildAction(builder);
            return this;
        }

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
