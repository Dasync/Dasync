using System.Reflection;
using System.Threading.Tasks;
using Dasync.ValueContainer;

namespace Dasync.Proxy
{
    public interface IProxyMethodExecutor
    {
        Task Execute<TParameters>(
            IProxy proxy,
            MethodInfo methodInfo,
            ref TParameters parameters)
            where TParameters : IValueContainer;
    }
}
