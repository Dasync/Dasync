using System;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Metadata
{
    public sealed class VersionSerializer : SerializerBase<Version, VersionContainer>
    {
        protected override VersionContainer Decompose(Version value)
            => new VersionContainer
            {
                Major = value.Major,
                Minor = value.Minor,
                Build = value.Build,
                Revision = value.Revision
            };

        protected override Version Compose(VersionContainer decomposition)
            => new Version(
                decomposition.Major,
                decomposition.Minor,
                decomposition.Build,
                decomposition.Revision);
    }

    public sealed class VersionContainer : ValueContainerBase
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;
    }
}
