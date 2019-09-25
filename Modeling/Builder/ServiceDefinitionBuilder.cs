using System;
using System.Linq.Expressions;

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

        public ServiceDefinitionBuilder AlternateName(params string[] alternateServiceNames)
        {
            foreach (var altName in alternateServiceNames)
                ServiceDefinition.AddAlternateName(altName);
            return this;
        }

        public MethodDefinitionBuilder Method(string methodName)
        {
            var methodDefinition = ServiceDefinition.GetMethod(methodName);
            methodDefinition.IsQuery = false;
            return new MethodDefinitionBuilder(methodDefinition);
        }

        public ServiceDefinitionBuilder Method(string methodName, Action<MethodDefinitionBuilder> buildAction)
        {
            buildAction(Method(methodName));
            return this;
        }

        public CommandDefinitionBuilder Command(string methodName)
        {
            var methodDefinition = ServiceDefinition.GetMethod(methodName);
            methodDefinition.IsQuery = false;
            return new CommandDefinitionBuilder((IMutableCommandDefinition)methodDefinition);
        }

        public ServiceDefinitionBuilder Command(string methodName, Action<CommandDefinitionBuilder> buildAction)
        {
            buildAction(Command(methodName));
            return this;
        }

        public CommandDefinitionBuilder Query(string methodName)
        {
            var methodDefinition = ServiceDefinition.GetMethod(methodName);
            methodDefinition.IsQuery = true;
            return new CommandDefinitionBuilder((IMutableCommandDefinition)methodDefinition);
        }

        public ServiceDefinitionBuilder Query(string methodName, Action<CommandDefinitionBuilder> buildAction)
        {
            buildAction(Query(methodName));
            return this;
        }
    }

    public class ServiceDefinitionBuilder<TImplementation> : ServiceDefinitionBuilder
    {
        public ServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
            : base(serviceDefinition)
        {
        }

        public MethodDefinitionBuilder Method(Expression<Func<TImplementation, string>> methodNameSelector)
        {
            var methodName = (string)((ConstantExpression)methodNameSelector.Body).Value;
            return Method(methodName);
        }

        public new ServiceDefinitionBuilder<TImplementation> Method(string methodName, Action<MethodDefinitionBuilder> buildAction)
        {
            buildAction(Method(methodName));
            return this;
        }

        public ServiceDefinitionBuilder<TImplementation> Method(Expression<Func<TImplementation, string>> methodNameSelector, Action<MethodDefinitionBuilder> buildAction)
        {
            var methodName = (string)((ConstantExpression)methodNameSelector.Body).Value;
            buildAction(Method(methodName));
            return this;
        }

        public CommandDefinitionBuilder Command(Expression<Func<TImplementation, string>> methodNameSelector)
        {
            var methodName = (string)((ConstantExpression)methodNameSelector.Body).Value;
            return Command(methodName);
        }

        public new ServiceDefinitionBuilder<TImplementation> Command(string methodName, Action<CommandDefinitionBuilder> buildAction)
        {
            buildAction(Command(methodName));
            return this;
        }

        public ServiceDefinitionBuilder<TImplementation> Command(Expression<Func<TImplementation, string>> methodNameSelector, Action<CommandDefinitionBuilder> buildAction)
        {
            var methodName = (string)((ConstantExpression)methodNameSelector.Body).Value;
            buildAction(Command(methodName));
            return this;
        }
    }
}
