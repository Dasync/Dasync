using System.Diagnostics;
using System.Reflection;

namespace Dasync.AsyncStateMachine
{
    [DebuggerDisplay("{Name}: {FieldInfo} {Type}")]
    public struct AsyncStateMachineField
    {
        public AsyncStateMachineFieldType Type { get; private set; }

        public FieldInfo FieldInfo { get; private set; }

        public string Name { get; private set; }

        public string InternalName { get; private set; }

        public AsyncStateMachineField(AsyncStateMachineFieldType type, FieldInfo field, string name, string internalName)
        {
            Type = type;
            FieldInfo = field;
            Name = name;
            InternalName = internalName;
        }
    }
}
