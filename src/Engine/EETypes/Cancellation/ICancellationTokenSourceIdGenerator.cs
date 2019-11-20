using System;

namespace Dasync.EETypes.Cancellation
{
    public interface ICancellationTokenSourceIdGenerator
    {
        Guid GenerateNewId();
    }
}
