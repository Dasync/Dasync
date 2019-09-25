using System.Threading.Tasks;
using Dasync.Modeling;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Resolvers
{
    public interface IMethodReference
    {
        MethodId Id { get; }

        IMethodDefinition Definition { get; }

        IValueContainer CreateParametersContainer();

        Task Invoke(object serviceInstance, IValueContainer parameters);
    }
}
