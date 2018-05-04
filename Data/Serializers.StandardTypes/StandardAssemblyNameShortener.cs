using System.Reflection;
using Dasync.Serialization;

namespace Dasync.Serializers.StandardTypes
{
    public sealed class StandardAssemblyNameShortener : IAssemblyNameShortener
    {
        private static readonly Assembly _mscorlibAssembly = typeof(object).GetAssembly();

        public bool TryShorten(Assembly assembly, out string shortName)
        {
            if (ReferenceEquals(assembly, _mscorlibAssembly))
            {
                shortName = _mscorlibAssembly.GetName().Name;
                return true;
            }

            shortName = null;
            return false;
        }

        public bool TryExpand(string shortName, out Assembly assembly)
        {
            if (string.Equals(shortName, _mscorlibAssembly.GetName().Name))
            {
                assembly = _mscorlibAssembly;
                return true;
            }

            assembly = null;
            return false;
        }
    }
}
