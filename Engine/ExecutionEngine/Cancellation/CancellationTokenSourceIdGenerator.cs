using System;
using Dasync.EETypes.Cancellation;

namespace Dasync.ExecutionEngine.Cancellation
{
    public class CancellationTokenSourceIdGenerator : ICancellationTokenSourceIdGenerator
    {
        public Guid GenerateNewId()
        {
#warning TODO: add better id generator - a sequential one
            return Guid.NewGuid();
        }
    }
}
