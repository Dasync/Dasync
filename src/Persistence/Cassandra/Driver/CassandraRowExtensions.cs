using System;

namespace Cassandra
{
    public static class CassandraRowExtensions
    {
        public static TimeSpan? GetDurationValue(this Row row, int columnIndex) =>
            ConvertDurationToTimeSpan(row[columnIndex]);

        public static TimeSpan? GetDurationValue(this Row row, string columnName) =>
            ConvertDurationToTimeSpan(row.GetValueOrDefault<object>(columnName));

        public static T GetValueOrDefault<T>(this Row row, string columnName, T defaultValue = default)
        {
            var column = row.GetColumn(columnName);
            if (column == null)
                return defaultValue;
            return row.GetValue<T>(column.Index);
        }

        private static TimeSpan? ConvertDurationToTimeSpan(object value)
        {
            if (value == null)
                return null;

            if (value is TimeSpan ts)
                return ts;

            if (value is byte[] bytes)
            {
                int i = 0;
                var months = VIntCoding.ReadUnsignedVInt(bytes, ref i);
                var days = VIntCoding.ReadUnsignedVInt(bytes, ref i);
                var nanoseconds = VIntCoding.ReadUnsignedVInt(bytes, ref i);
                var ticks = nanoseconds / 100;
                var result = TimeSpan.FromTicks(ticks);
                if (days > 0)
                    result += TimeSpan.FromDays(days);
                if (months > 0)
                    result += TimeSpan.FromDays(months * 30);
                return result;
            }

            throw new ArgumentException($"Cannot convert '{value.GetType()}' to '{typeof(TimeSpan)}'.");
        }
    }
}
