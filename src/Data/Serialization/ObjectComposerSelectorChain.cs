using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Serialization
{
    public class ObjectComposerSelectorChain : IObjectComposerSelector
    {
        private readonly IObjectComposerSelector[] _composerSelectors;

        public ObjectComposerSelectorChain(params IObjectComposerSelector[] decomposerSelectors)
            : this((IEnumerable<IObjectComposerSelector>)decomposerSelectors)
        {
        }

        public ObjectComposerSelectorChain(IEnumerable<IObjectComposerSelector> composerSelectors)
        {
            _composerSelectors = composerSelectors as IObjectComposerSelector[] ?? composerSelectors.ToArray();
        }

        public IObjectComposer SelectComposer(Type type)
        {
            for (var i = 0; i < _composerSelectors.Length; i++)
            {
                var composer = _composerSelectors[i].SelectComposer(type);
                if (composer != null)
                    return composer;
            }

            return null;
        }
    }
}
