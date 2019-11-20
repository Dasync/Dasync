using System.Threading.Tasks;
using Dasync.ValueContainer;

namespace Dasync.Proxy
{
    public interface IMethodInvoker
    {
        IValueContainer CreateParametersContainer();

        Task Invoke(object instance, IValueContainer parameters);
    }
}