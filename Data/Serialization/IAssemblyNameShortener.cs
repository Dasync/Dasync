using System.Reflection;

namespace Dasync.Serialization
{
#warning Use AssemblySerializationInfo instead of Assembly
    public interface IAssemblyNameShortener
    {
        bool TryShorten(Assembly assembly, out string shortName);

        bool TryExpand(string shortName, out Assembly assembly);
    }
}
