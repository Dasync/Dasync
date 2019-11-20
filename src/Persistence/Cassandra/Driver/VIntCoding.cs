namespace Cassandra
{
    internal static class VIntCoding
    {
        public static long ReadUnsignedVInt(byte[] input, ref int index)
        {
            int firstByte = unchecked((sbyte)input[index++]);

            if (firstByte >= 0)
                return firstByte >> 1;

            int size = NumberOfExtraBytesToRead(firstByte);
            long retval = firstByte & FirstByteValueMask(size);
            for (int ii = 0; ii < size; ii++)
            {
                long b = unchecked((sbyte)input[index++]);
                retval <<= 8;
                retval |= b & 0xff;
            }

            return retval >> 1;
        }

        private static int FirstByteValueMask(int extraBytesToRead)
        {
            return 0xff >> extraBytesToRead;
        }

        private static int NumberOfExtraBytesToRead(int firstByte)
        {
            return NumberOfLeadingZeros(~firstByte) - 24;
        }

        private static int NumberOfLeadingZeros(int x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x -= x >> 1 & 0x55555555;
            x = (x >> 2 & 0x33333333) + (x & 0x33333333);
            x = (x >> 4) + x & 0x0f0f0f0f;
            x += x >> 8;
            x += x >> 16;
            return 32 - (x & 0x0000003f);
        }
    }
}
