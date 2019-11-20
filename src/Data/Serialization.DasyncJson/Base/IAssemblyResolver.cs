using System.Reflection;

namespace Dasync.Serialization
{
    public interface IAssemblyResolver
    {
        Assembly Resolve(AssemblySerializationInfo info);
    }
}
