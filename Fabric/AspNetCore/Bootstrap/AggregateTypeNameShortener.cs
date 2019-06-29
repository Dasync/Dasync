using Dasync.Serialization;
using Dasync.Serializers.DomainTypes;
using Dasync.Serializers.EETypes;
using Dasync.Serializers.StandardTypes;

namespace Dasync.Bootstrap
{
    public class AggregateTypeNameShortener : TypeNameShortenerChain
    {
        public AggregateTypeNameShortener(
            StandardTypeNameShortener standardTypeNameShortener,
            EETypesNameShortener eeTypesNameShortener,
            DomainTypesNameShortener domainTypesNameShortener)
            : base(standardTypeNameShortener, eeTypesNameShortener, domainTypesNameShortener)
        {
        }
    }
}
