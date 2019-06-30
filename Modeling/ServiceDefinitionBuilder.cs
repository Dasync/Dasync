using System;

namespace Dasync.Modeling
{
    public class ServiceDefinitionBuilder
    {
        public static ServiceDefinitionBuilder CreateByImplementationType(
            Type serviceImplementationType, IMutableServiceDefinition serviceDefinition) =>
            (ServiceDefinitionBuilder)Activator.CreateInstance(
                typeof(ServiceDefinitionBuilder<>).MakeGenericType(serviceImplementationType),
                new object[] { serviceDefinition });

        public ServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
        {
            ServiceDefinition = serviceDefinition;
        }

        public IMutableServiceDefinition ServiceDefinition { get; private set; }

        public ServiceDefinitionBuilder Name(string serviceName)
        {
            ServiceDefinition.Name = serviceName;
            return this;
        }
    }

    public class ServiceDefinitionBuilder<TImplementation> : ServiceDefinitionBuilder
    {
        public ServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
            : base(serviceDefinition)
        {
        }

    }
}
