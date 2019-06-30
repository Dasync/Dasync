using System;

namespace Dasync.Modeling
{
    public class ExternalServiceDefinitionBuilder
    {
        public static ExternalServiceDefinitionBuilder CreateByInterfaceType(
            Type serviceInterfaceType, IMutableServiceDefinition serviceDefinition) =>
            (ExternalServiceDefinitionBuilder)Activator.CreateInstance(
                typeof(ExternalServiceDefinitionBuilder<>).MakeGenericType(serviceInterfaceType),
                new object[] { serviceDefinition });

        public ExternalServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
        {
            ServiceDefinition = serviceDefinition;
        }

        public IMutableServiceDefinition ServiceDefinition { get; private set; }

        public ExternalServiceDefinitionBuilder Name(string serviceName)
        {
            ServiceDefinition.Name = serviceName;
            return this;
        }

        public ExternalServiceDefinitionBuilder AlternativeName(params string[] alternativeServiceNames)
        {
            foreach (var altName in alternativeServiceNames)
                ServiceDefinition.AddAlternativeName(altName);
            return this;
        }
    }

    public class ExternalServiceDefinitionBuilder<TInterface> : ExternalServiceDefinitionBuilder
    {
        public ExternalServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
            : base(serviceDefinition)
        {
        }

    }
}
