using System.Reflection;
using Dasync.EETypes.Proxy;
using Dasync.Serialization;

namespace Dasync.Serializers.EETypes
{
    public sealed class EEAssemblyNameShortener : IAssemblyNameShortener
    {
        private static readonly Assembly _eetypesAssembly = typeof(ServiceProxyContext).GetAssembly();

        public bool TryShorten(Assembly assembly, out string shortName)
        {
            if (ReferenceEquals(assembly, _eetypesAssembly))
            {
                shortName = "dasynceetypes";
                return true;
            }

            shortName = null;
            return false;
        }

        public bool TryExpand(string shortName, out Assembly assembly)
        {
            if (string.Equals(shortName, "dasynceetypes", System.StringComparison.Ordinal))
            {
                assembly = _eetypesAssembly;
                return true;
            }

            assembly = null;
            return false;
        }
    }
}
