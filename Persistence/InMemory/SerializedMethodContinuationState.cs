using Dasync.EETypes.Persistence;

namespace Dasync.Persistence.InMemory
{
    public class SerializedMethodContinuationState : ISerializedMethodContinuationState
    {
        public string ContentType { get; set; }

        public byte[] State { get; set; }
    }
}
