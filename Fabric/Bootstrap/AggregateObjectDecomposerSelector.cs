using Dasync.Serialization;
using Dasync.Serializers.EETypes;
using Dasync.Serializers.StandardTypes;

namespace Dasync.Bootstrap
{
    public class AggregateObjectDecomposerSelector : ObjectDecomposerSelectorChain
    {
        public AggregateObjectDecomposerSelector(
            EETypesSerializerSelector eeTypesSerializerSelector,
            StandardTypeDecomposerSelector standardTypeDecomposerSelector)
            : base(eeTypesSerializerSelector, standardTypeDecomposerSelector)
        {
        }
    }
}
