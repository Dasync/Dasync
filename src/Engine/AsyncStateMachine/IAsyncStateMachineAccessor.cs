using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dasync.AsyncStateMachine
{
    public interface IAsyncStateMachineAccessor
    {
        IAsyncStateMachine CreateInstance();

        Task GetCompletionTask(IAsyncStateMachine stateMachine);
    }
}
