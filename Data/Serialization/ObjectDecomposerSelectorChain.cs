using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Serialization
{
    public class ObjectDecomposerSelectorChain : IObjectDecomposerSelector
    {
        private readonly IObjectDecomposerSelector[] _decomposerSelectors;

        public ObjectDecomposerSelectorChain(params IObjectDecomposerSelector[] decomposerSelectors)
            : this((IEnumerable<IObjectDecomposerSelector>)decomposerSelectors)
        {
        }

        public ObjectDecomposerSelectorChain(IEnumerable<IObjectDecomposerSelector> decomposerSelectors)
        {
            _decomposerSelectors = decomposerSelectors as IObjectDecomposerSelector[] ?? decomposerSelectors.ToArray();
        }

        public IObjectDecomposer SelectDecomposer(Type type)
        {
            for (var i = 0; i < _decomposerSelectors.Length; i++)
            {
                var decomposer = _decomposerSelectors[i].SelectDecomposer(type);
                if (decomposer != null)
                    return decomposer;
            }

            return null;
        }
    }
}
