using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Communication
{
    public interface IMethodInvocationData : IInvocationData
    {
        ServiceId Service { get; }

        MethodId Method { get; }

        ContinuationDescriptor Continuation { get; }

        Task ReadInputParameters(IValueContainer target);
    }
}
