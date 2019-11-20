using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dasync.Serialization
{
    public class AssemblyNameShortenerChain : IAssemblyNameShortener
    {
        private readonly IAssemblyNameShortener[] _chain;

        public AssemblyNameShortenerChain(params IAssemblyNameShortener[] chain)
            : this((IEnumerable<IAssemblyNameShortener>)chain)
        {
        }

        public AssemblyNameShortenerChain(IEnumerable<IAssemblyNameShortener> chain)
        {
            _chain = chain as IAssemblyNameShortener[] ?? chain.ToArray();
        }

        public bool TryShorten(Assembly assembly, out string shortName)
        {
            foreach (var shortener in _chain)
                if (shortener.TryShorten(assembly, out shortName))
                    return true;
            shortName = null;
            return false;
        }

        public bool TryExpand(string shortName, out Assembly assembly)
        {
            foreach (var shortener in _chain)
                if (shortener.TryExpand(shortName, out assembly))
                    return true;
            assembly = null;
            return false;
        }
    }
}
