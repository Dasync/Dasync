using System.Threading;
using System.Threading.Tasks;

namespace Dasync.EETypes.Communication
{
    public interface ISynchronousCommunicator : ICommunicator
    {
        Task<InvokeRoutineResult> GetInvocationResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            CancellationToken ct);
    }
}
