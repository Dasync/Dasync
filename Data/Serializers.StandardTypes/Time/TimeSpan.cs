using System;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Time
{
    public sealed class TimeSpanSerializer : SerializerBase<TimeSpan, TimeSpanContainer>
    {
        protected override TimeSpanContainer Decompose(TimeSpan value)
            => new TimeSpanContainer
            {
                Ticks = value.Ticks
            };

        protected override TimeSpan Compose(TimeSpanContainer decomposition)
            => TimeSpan.FromTicks(decomposition.Ticks);
    }

    public sealed class TimeSpanContainer : ValueContainerBase
    {
        public long Ticks;
    }
}
