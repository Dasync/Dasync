using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dasync.AsyncStateMachine
{
    public class AsyncStateMachineAccessor : IAsyncStateMachineAccessor
    {
        private Func<IAsyncStateMachine> _createInstanceFunc;
        private Func<IAsyncStateMachine, Task> _getCompletionTaskFunc;

        public AsyncStateMachineAccessor(AsyncStateMachineMetadata metadata)
        {
            _createInstanceFunc = CompileCreateInstanceFunc(metadata);
            _getCompletionTaskFunc = CompileGetCompletionTaskFunc(metadata);
        }

        public IAsyncStateMachine CreateInstance()
        {
            return _createInstanceFunc();
        }

        public Task GetCompletionTask(IAsyncStateMachine stateMachine)
        {
            return _getCompletionTaskFunc(stateMachine);
        }

        private static Func<IAsyncStateMachine> CompileCreateInstanceFunc(AsyncStateMachineMetadata metadata)
        {
            var stateMachine = Expression.Variable(metadata.StateMachineType, "sm");
            var newInstance = Expression.New(metadata.StateMachineType);
            var newStateMachine = Expression.Assign(stateMachine, newInstance);
            var newBuilder = Expression.Call(null, metadata.Builder.FieldInfo.FieldType.GetMethod(nameof(AsyncTaskMethodBuilder.Create)));
            var setBuilder = Expression.Assign(Expression.MakeMemberAccess(stateMachine, metadata.Builder.FieldInfo), newBuilder);
            var setState = Expression.Assign(Expression.MakeMemberAccess(stateMachine, metadata.State.FieldInfo), Expression.Constant(-1));
            var funcBody = Expression.Block(new[] { stateMachine }, newStateMachine, setBuilder, setState, Expression.Convert(stateMachine, typeof(IAsyncStateMachine)));
            var lambda = Expression.Lambda(funcBody);
            return (Func<IAsyncStateMachine>)lambda.Compile();
        }

        private static Func<IAsyncStateMachine, Task> CompileGetCompletionTaskFunc(AsyncStateMachineMetadata metadata)
        {
            var stateMachineArg = Expression.Variable(typeof(IAsyncStateMachine), "sm");
            var stateMachine = Expression.Convert(stateMachineArg, metadata.StateMachineType);
            var accessBuilder = Expression.MakeMemberAccess(stateMachine, metadata.Builder.FieldInfo);
            var taskProperty = metadata.Builder.FieldInfo.FieldType.GetProperty(nameof(AsyncTaskMethodBuilder.Task));
            var getTask = Expression.MakeMemberAccess(accessBuilder, taskProperty);
            var lambda = Expression.Lambda(getTask, stateMachineArg);
            return (Func<IAsyncStateMachine, Task>)lambda.Compile();
        }
    }
}
