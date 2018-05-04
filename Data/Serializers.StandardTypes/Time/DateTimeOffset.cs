using System;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Time
{
    public sealed class DateTimeOffsetSerializer : SerializerBase<DateTimeOffset, DateTimeOffsetContainer>
    {
        protected override DateTimeOffsetContainer Decompose(DateTimeOffset value)
            => new DateTimeOffsetContainer
            {
                Ticks = value.UtcTicks,
                Offset = value.Offset.Ticks
            };

        protected override DateTimeOffset Compose(DateTimeOffsetContainer decomposition)
            => new DateTimeOffset(decomposition.Ticks, TimeSpan.FromTicks(decomposition.Offset));
    }

    public sealed class DateTimeOffsetContainer : ValueContainerBase
    {
        public long Ticks;
        public long Offset;
    }
}
