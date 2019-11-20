using System.Reflection;

namespace Dasync.EETypes
{
    public interface IMethodIdProvider
    {
        MethodId GetId(MethodInfo methodInfo);
    }
}
