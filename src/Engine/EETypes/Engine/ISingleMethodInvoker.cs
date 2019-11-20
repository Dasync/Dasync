using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Engine
{
    /// <summary>
    /// Invokes (and petentially runs in place) a single method outside a transaction.
    /// </summary>
    public interface ISingleMethodInvoker
    {
        Task<InvokeRoutineResult> InvokeAsync(ExecuteRoutineIntent intent);
    }
}
