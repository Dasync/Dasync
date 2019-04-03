using System;
using Dasync.EETypes;

namespace Dasync.ExecutionEngine
{
    public class UniqueIdGenerator : IUniqueIdGenerator
    {
        private readonly Random _random = new Random();

        private static string Alphabet = "abcdefghklmnpqrstuvwxyz123456789";

        public string NewId()
        {
            unsafe
            {
                unchecked
                {
                    char* result = stackalloc char[17];
                    result[16] = '\0';

                    var lo = _random.Next();
                    var salt = _random.Next() & 1023;
                    var hi = (DateTime.UtcNow.Ticks << 3) ^ salt;

                    result[0] = Alphabet[(int)((hi >> 59) & 31)];
                    result[1] = Alphabet[(int)((hi >> 54) & 31)];
                    result[2] = Alphabet[(int)((hi >> 49) & 31)];
                    result[3] = Alphabet[(int)((hi >> 44) & 31)];
                    result[4] = Alphabet[(int)((hi >> 39) & 31)];
                    result[5] = Alphabet[(int)((hi >> 34) & 31)];
                    result[6] = Alphabet[(int)((hi >> 29) & 31)];
                    result[7] = Alphabet[(int)((hi >> 24) & 31)];
                    result[8] = Alphabet[(int)((hi >> 19) & 31)];
                    result[9] = Alphabet[(int)((hi >> 14) & 31)];
                    result[10] = Alphabet[(int)((hi >> 9) & 31)];
                    result[11] = Alphabet[(int)((hi >> 4) & 31)];
                    result[12] = Alphabet[(lo >> 15) & 31];
                    result[13] = Alphabet[(lo >> 10) & 31];
                    result[14] = Alphabet[(lo >> 5) & 31];
                    result[15] = Alphabet[lo & 31];

                    return new string(result);
                }
            }
        }
    }
}
