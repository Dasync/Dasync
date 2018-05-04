using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dasync.AsyncStateMachine
{
    public class AsyncStateMachineMetadataBuilder : IAsyncStateMachineMetadataBuilder
    {
        public AsyncStateMachineMetadata Build(Type stateMachineType)
        {
            if (stateMachineType == null)
                throw new ArgumentNullException(nameof(stateMachineType));

            var fields = stateMachineType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var inputArguments = new List<AsyncStateMachineField>();
            var localVariables = new List<AsyncStateMachineField>();

            AsyncStateMachineField? owner = null;
            AsyncStateMachineField? state = null;
            AsyncStateMachineField? builder = null;
            Type builderType = null;

            var isCompilerGeneratedAsm = stateMachineType
#if NETSTANDARD
                .GetTypeInfo()
#endif
                .GetCustomAttribute<CompilerGeneratedAttribute>() != null;

            foreach (var fi in fields)
            {
                bool isCompilerGeneratedField = false;
                string name = fi.Name;
                string internalName = name;

                if (name[0] == '<')
                {
                    isCompilerGeneratedField = true;
                    var closingBracketIndex = fi.Name.IndexOf('>');
                    name = fi.Name.Substring(1, closingBracketIndex - 1);
                    internalName = fi.Name.Substring(closingBracketIndex + 1);
                }

                if ((isCompilerGeneratedField && name.Length == 0 && fi.Name.EndsWith("__this"))
                    || (!isCompilerGeneratedField && fi.Name == "__this"))
                {
                    if (isCompilerGeneratedField)
                        name = "__this";
                    owner = new AsyncStateMachineField(AsyncStateMachineFieldType.OwnerReference, fi, name, internalName);
                }
                else if (fi.FieldType == typeof(int) && (
                    (isCompilerGeneratedField && name.Length == 0 && fi.Name.EndsWith("__state")) ||
                    (!isCompilerGeneratedField && fi.Name == "__state")))
                {
                    if (isCompilerGeneratedField)
                        name = "__state";
                    state = new AsyncStateMachineField(AsyncStateMachineFieldType.State, fi, name, internalName);
                }
                else if (/*name.Length == 0 && fi.Name.EndsWith("__builder") &&*/
                    (fi.FieldType == typeof(AsyncTaskMethodBuilder) ||
                        (fi.FieldType.GetTypeInfo().IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(AsyncTaskMethodBuilder<>))))
                {
                    if (isCompilerGeneratedField)
                        name = "__builder";
                    builder = new AsyncStateMachineField(AsyncStateMachineFieldType.Builder, fi, name, internalName);
                    builderType = fi.FieldType;
                }
                else if (isCompilerGeneratedField || !isCompilerGeneratedAsm)
                {
#warning Need to solve problem when variables have the same name? e.g. "<myVar>5__1" and "<myVar>5__2". They must be in different scopes.
                    localVariables.Add(new AsyncStateMachineField(
                        AsyncStateMachineFieldType.LocalVariable,
                        fi, name, internalName));
                }
                else
                {
                    inputArguments.Add(new AsyncStateMachineField(
                        AsyncStateMachineFieldType.InputArgument,
                        fi, name, internalName));
                }
            }

            return new AsyncStateMachineMetadata(
                stateMachineType,
                owner ?? default(AsyncStateMachineField),
                state ?? default(AsyncStateMachineField),
                builder ?? default(AsyncStateMachineField),
                inputArgs: inputArguments.ToArray(),
                localVars: localVariables.ToArray());
        }
    }
}
