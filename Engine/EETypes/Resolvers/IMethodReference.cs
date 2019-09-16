using Dasync.Modeling;

namespace Dasync.EETypes.Resolvers
{
    public interface IMethodReference
    {
        RoutineMethodId Id { get; }

        IMethodDefinition Definition { get; }
    }
}
