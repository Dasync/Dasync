using Dasync.Serialization;
using Dasync.Serializers.EETypes;
using Dasync.Serializers.StandardTypes;

namespace Dasync.Bootstrap
{
    public class AggregateObjectComposerSelector : ObjectComposerSelectorChain
    {
        public AggregateObjectComposerSelector(
            EETypesSerializerSelector eeTypesSerializerSelector,
            StandardTypeComposerSelector standardTypeComposerSelector)
            : base(eeTypesSerializerSelector, standardTypeComposerSelector)
        {
        }
    }
}
