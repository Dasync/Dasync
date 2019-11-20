using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Dasync.Proxy;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class MethodReference : IMethodReference
    {
        private readonly IMethodInvoker _invoker;

        public MethodReference(MethodId id, IMethodDefinition definition, IMethodInvoker invoker)
        {
            Id = id;
            Definition = definition;
            _invoker = invoker;
        }

        public MethodId Id { get; }

        public IMethodDefinition Definition { get; }

        public IValueContainer CreateParametersContainer() => _invoker.CreateParametersContainer();

        public Task Invoke(object serviceInstance, IValueContainer parameters) => _invoker.Invoke(serviceInstance, parameters);
    }
}