using System.Reflection;

namespace Dasync.Proxy
{
    public interface IMethodInvokerFactory
    {
        IMethodInvoker Create(MethodInfo methodInfo);
    }
}
