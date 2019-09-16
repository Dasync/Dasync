using System.Reflection;
using Dasync.Modeling;

namespace Dasync.EETypes
{
    public interface IRoutineMethodResolver
    {
        MethodInfo Resolve(IServiceDefinition serviceDefinition, RoutineMethodId methodId);
    }
}
