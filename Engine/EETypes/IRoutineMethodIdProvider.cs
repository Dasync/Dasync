using System.Reflection;

namespace Dasync.EETypes
{
    public interface IRoutineMethodIdProvider
    {
        RoutineMethodId GetId(MethodInfo methodInfo);
    }
}
