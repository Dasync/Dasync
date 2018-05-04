using Dasync.Serialization;
using Dasync.Serializers.EETypes;
using Dasync.Serializers.StandardTypes;

namespace Dasync.Bootstrap
{
    public class AggregateTypeNameShortener : TypeNameShortenerChain
    {
        public AggregateTypeNameShortener(
            StandardTypeNameShortener standardTypeNameShortener,
            EETypesNameShortener eeTypesNameShortener)
            : base(standardTypeNameShortener, eeTypesNameShortener)
        {
        }
    }
}
