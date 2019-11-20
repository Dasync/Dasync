using System;
using System.Text;

namespace Dasync.Persistence.Cassandra
{
    public static class StringBuilderCqlExtensions
    {
        public static StringBuilder AppendCqlText(this StringBuilder sb, string value)
        {
            return sb.Append('\'').Append(value).Append('\'');
        }

        public static StringBuilder AppendCqlTimestamp(this StringBuilder sb, DateTimeOffset value)
        {
            return sb.Append('\'').Append(value.ToString("yyyy-MM-ddTHH:mm:ss.FFFzz")).Append(value.Offset.Minutes.ToString("D2")).Append('\'');
        }

        public static StringBuilder AppendCqlDuration(this StringBuilder sb, TimeSpan value)
        {
            return sb.Append(value.Ticks / 10).Append("us");
        }

        public static StringBuilder AppendCqlBlob(this StringBuilder sb, byte[] value)
        {
            sb.Append("0x");

            foreach (var b in value)
            {
                var high = b >> 4;
                if (high > 9)
                    sb.Append((char)('A' + high - 10));
                else
                    sb.Append(high);

                var low = b & 0x0f;
                if (low > 9)
                    sb.Append((char)('A' + low - 10));
                else
                    sb.Append(low);
            }

            return sb;
        }
    }
}
