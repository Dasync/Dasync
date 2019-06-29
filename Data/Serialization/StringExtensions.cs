using System;

namespace Dasync.Serialization
{
    public static class StringExtensions
    {
        public static byte[] ParseAsHexByteArray(this string value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return new byte[0];

            if ((value.Length & 1) != 0)
                throw new ArgumentException($"The length of a hexadecimal string must be even, but got {value.Length}.");

            var result = new byte[value.Length >> 1];

            for (var i = 0; i < result.Length; i++)
            {
#warning optimize
                var @byte = byte.Parse(value.Substring(i << 1, 2), System.Globalization.NumberStyles.HexNumber);
                result[i] = @byte;
            }

            return result;
        }
    }
}