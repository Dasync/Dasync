using System;
using System.Reflection;
using Dasync.ValueContainer;

namespace Dasync.Serializers.StandardTypes.Time
{
    public sealed class DateTimeSerializer : SerializerBase<DateTime, DateTimeContainer>
    {
        protected override DateTimeContainer Decompose(DateTime value)
            => new DateTimeContainer
            {
                Ticks = value.Ticks,
                Kind = (sbyte)value.Kind
            };

        protected override DateTime Compose(DateTimeContainer decomposition)
            => new DateTime(decomposition.Ticks, (DateTimeKind)decomposition.Kind);
    }

    public struct DateTimeContainer : IStronglyTypedValueContainer
    {
        public long Ticks;
        public sbyte Kind;

        #region IValueContainer

        public int GetCount() => 2;

        public string GetName(int index)
        {
            switch (index)
            {
                case 0: return nameof(Ticks);
                case 1: return nameof(Kind);
                default: throw new IndexOutOfRangeException();
            }
        }

        public Type GetType(int index)
        {
            switch (index)
            {
                case 0: return typeof(long);
                case 1: return typeof(sbyte);
                default: throw new IndexOutOfRangeException();
            }
        }

        public object GetValue(int index)
        {
            switch (index)
            {
                case 0: return Ticks;
                case 1: return Kind;
                default: throw new IndexOutOfRangeException();
            }
        }

        public void SetValue(int index, object value)
        {
            switch (index)
            {
                case 0: Ticks = (long)value; return;
                case 1: Kind = (sbyte)value; return;
                default: throw new IndexOutOfRangeException();
            }
        }

        public MemberInfo GetMember(int index)
        {
            switch (index)
            {
                case 0: return _ticksMemberInfo;
                case 1: return _kindMemberInfo;
                default: throw new IndexOutOfRangeException();
            }
        }

        private static readonly MemberInfo _ticksMemberInfo =
            typeof(DateTimeContainer).GetField(nameof(Ticks));

        private static readonly MemberInfo _kindMemberInfo =
            typeof(DateTimeContainer).GetField(nameof(Kind));

        #endregion
    }
}