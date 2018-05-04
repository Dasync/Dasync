using System.Reflection;
using Dasync.EETypes;

namespace Dasync.ExecutionEngine.Intents
{
    public class RoutineMethodIdProvider : IRoutineMethodIdProvider
    {
        public RoutineMethodId GetId(MethodInfo methodInfo)
        {
            return new RoutineMethodId
            {
                MethodName = methodInfo.Name
#warning Add method generic arguments
#warning Add signature info for method overload resolution
#warning Add versioning info
            };
        }
    }
}
