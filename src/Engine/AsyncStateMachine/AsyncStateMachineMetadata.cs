using System;

namespace Dasync.AsyncStateMachine
{
    public sealed class AsyncStateMachineMetadata
    {
        public Type StateMachineType { get; }

        public AsyncStateMachineField Owner { get; }

        public AsyncStateMachineField State { get; }

        public AsyncStateMachineField Builder { get; }

        public AsyncStateMachineField[] InputArguments { get; }

        public AsyncStateMachineField[] LocalVariables { get; }

        public AsyncStateMachineMetadata(
            Type stateMachineType,
            AsyncStateMachineField owner,
            AsyncStateMachineField state,
            AsyncStateMachineField builder,
            AsyncStateMachineField[] inputArgs,
            AsyncStateMachineField[] localVars)
        {
            StateMachineType = stateMachineType;
            Owner = owner;
            State = state;
            Builder = builder;
            InputArguments = inputArgs;
            LocalVariables = localVars;
        }
    }
}
