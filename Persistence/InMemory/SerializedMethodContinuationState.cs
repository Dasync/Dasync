using Dasync.EETypes.Persistence;

namespace Dasync.Persistence.InMemory
{
    public class SerializedMethodContinuationState : ISerializedMethodContinuationState
    {
        public string Format { get; set; }

        public byte[] State { get; set; }
    }
}
