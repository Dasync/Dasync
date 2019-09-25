using System.Reflection;
using Dasync.EETypes;

namespace Dasync.ExecutionEngine
{
    public class MethodIdProvider : IMethodIdProvider
    {
        public MethodId GetId(MethodInfo methodInfo)
        {
            return new MethodId
            {
                Name = methodInfo.Name
#warning Add method generic arguments
#warning Add signature info for method overload resolution
#warning Add versioning info
            };
        }
    }
}
