using Dasync.EETypes;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class MethodReference : IMethodReference
    {
        public MethodReference(RoutineMethodId id, IMethodDefinition definition)
        {
            Id = id;
            Definition = definition;
        }

        public RoutineMethodId Id { get; }

        public IMethodDefinition Definition { get; }
    }
}