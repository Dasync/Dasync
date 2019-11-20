using System;
using System.Linq.Expressions;

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

        public ExternalServiceDefinitionBuilder AlternateName(params string[] alternateServiceNames)
        {
            foreach (var altName in alternateServiceNames)
                ServiceDefinition.AddAlternateName(altName);
            return this;
        }

        public MethodDefinitionBuilder Method(string methodName)
        {
            MethodDefinition methodDefinition = null;
            return new MethodDefinitionBuilder(methodDefinition);
        }

        public ExternalServiceDefinitionBuilder Method(string methodName, Action<MethodDefinitionBuilder> buildAction)
        {
            buildAction(Method(methodName));
            return this;
        }
    }

    public class ExternalServiceDefinitionBuilder<TInterface> : ExternalServiceDefinitionBuilder
    {
        public ExternalServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
            : base(serviceDefinition)
        {
        }

        public MethodDefinitionBuilder Method(Expression<Func<TInterface, string>> methodNameSelector)
        {
            var methodName = (string)((ConstantExpression)methodNameSelector.Body).Value;
            return Method(methodName);
        }

        public new ExternalServiceDefinitionBuilder<TInterface> Method(string methodName, Action<MethodDefinitionBuilder> buildAction)
        {
            buildAction(Method(methodName));
            return this;
        }

        public ExternalServiceDefinitionBuilder<TInterface> Method(Expression<Func<TInterface, string>> methodNameSelector, Action<MethodDefinitionBuilder> buildAction)
        {
            var methodName = (string)((ConstantExpression)methodNameSelector.Body).Value;
            buildAction(Method(methodName));
            return this;
        }
    }
}
