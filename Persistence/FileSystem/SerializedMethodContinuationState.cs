using Dasync.EETypes.Persistence;

namespace Dasync.Persistence.FileSystem
{
    public class SerializedMethodContinuationState : ISerializedMethodContinuationState
    {
        public string ContentType { get; set; }

        public byte[] State { get; set; }
    }
}
