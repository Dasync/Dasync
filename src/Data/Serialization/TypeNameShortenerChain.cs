using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Serialization
{
    public class TypeNameShortenerChain : ITypeNameShortener
    {
        private readonly ITypeNameShortener[] _chain;

        public TypeNameShortenerChain(params ITypeNameShortener[] chain)
            : this((IEnumerable<ITypeNameShortener>)chain)
        {
        }

        public TypeNameShortenerChain(IEnumerable<ITypeNameShortener> chain)
        {
            _chain = chain as ITypeNameShortener[] ?? chain.ToArray();
        }

        public bool TryShorten(Type type, out string shortName)
        {
            foreach (var shortener in _chain)
                if (shortener.TryShorten(type, out shortName))
                    return true;
            shortName = null;
            return false;
        }

        public bool TryExpand(string shortName, out Type type)
        {
            foreach (var shortener in _chain)
                if (shortener.TryExpand(shortName, out type))
                    return true;
            type = null;
            return false;
        }
    }
}
