using Dasync.EETypes.Persistence;

namespace Dasync.Communication.InMemory
{
    public class SerializedMethodContinuationState : ISerializedMethodContinuationState
    {
        public string Format { get; set; }

        public byte[] State { get; set; }
    }
}
