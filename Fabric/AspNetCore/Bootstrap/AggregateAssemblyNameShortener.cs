using Dasync.Serialization;
using Dasync.Serializers.EETypes;
using Dasync.Serializers.StandardTypes;

namespace Dasync.Bootstrap
{
    public class AggregateAssemblyNameShortener : AssemblyNameShortenerChain
    {
        public AggregateAssemblyNameShortener(
            StandardAssemblyNameShortener standardAssemblyNameShortener,
            EEAssemblyNameShortener eeAssemblyNameShortener)
            : base(standardAssemblyNameShortener, eeAssemblyNameShortener)
        {
        }
    }
}
