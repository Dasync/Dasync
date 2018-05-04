using System;
using Dasync.EETypes;

namespace Dasync.ExecutionEngine
{
    public class NumericIdGenerator : INumericIdGenerator
    {
        private Random _random = new Random();

        public long NewId() => (DateTime.UtcNow.Ticks << 3) ^ (_random.Next() & 0x07_ff_ff);
    }
}
