using System.Reflection;

namespace Dasync.ExecutionEngine.StateMetadata.Service
{
    public class ServiceStateVariable
    {
        public string Name { get; private set; }

        public FieldInfo Field { get; private set; }

        public ServiceStateVariable(string name, FieldInfo fieldInfo)
        {
            Name = name;
            Field = fieldInfo;
        }
    }
}
