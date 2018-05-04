using System.Linq;

namespace Dasync.Serialization
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] array)
        {
            if (array == null)
                return null;

            if (array.Length == 0)
                return string.Empty;

#warning optimize
            return string.Concat(array.Select(b => b.ToString("X2")));
        }
    }
}
